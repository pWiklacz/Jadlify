using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using Jadlify.API.Session;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Jadlify.API.Tests.Authentication;

/// <summary>
/// Exercises the asymmetric (ES256) JWT validation path through the real <c>JwtBearer</c>
/// middleware — the same path production and a local Supabase CLI stack use (Supabase signs
/// user access tokens with an ECDSA P-256 key and publishes the public key via JWKS).
/// Unlike <see cref="AuthBoundaryTests"/>, this does NOT swap in a test authentication handler:
/// tokens are minted in-process with an in-test EC key and validated by the production pipeline.
/// The in-test EC public key is injected directly as the signing key so no live JWKS endpoint is
/// needed (JWKS/<c>kid</c> resolution against a real stack is covered by manual verification).
/// </summary>
public sealed class AsymmetricJwtValidationTests : IDisposable
{
    private const string Issuer = "http://127.0.0.1:54421/auth/v1";
    private const string Audience = "authenticated";

    private readonly ECDsa _signingAlgorithm = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    private readonly ECDsa _wrongAlgorithm = ECDsa.Create(ECCurve.NamedCurves.nistP256);

    public void Dispose()
    {
        _signingAlgorithm.Dispose();
        _wrongAlgorithm.Dispose();
    }

    [Fact]
    public async Task Me_ShouldReturnSubject_WhenTokenIsWellFormedAndUnexpired()
    {
        using TestApiFactory factory = new(new ECDsaSecurityKey(_signingAlgorithm));
        using HttpClient client = factory.CreateHttpsClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateToken(_signingAlgorithm, subject: "user-123"));

        HttpResponseMessage response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        MeResponse? body = await response.Content.ReadFromJsonAsync<MeResponse>();
        Assert.NotNull(body);
        Assert.Equal("user-123", body.UserId);
    }

    [Fact]
    public async Task Me_ShouldReturnUnauthorized_WhenTokenIsSignedWithWrongKey()
    {
        using TestApiFactory factory = new(new ECDsaSecurityKey(_signingAlgorithm));
        using HttpClient client = factory.CreateHttpsClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateToken(_wrongAlgorithm, subject: "user-123"));

        HttpResponseMessage response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_ShouldReturnUnauthorized_WhenTokenIsExpired()
    {
        using TestApiFactory factory = new(new ECDsaSecurityKey(_signingAlgorithm));
        using HttpClient client = factory.CreateHttpsClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateToken(
                _signingAlgorithm,
                subject: "user-123",
                notBefore: DateTime.UtcNow.AddHours(-2),
                expires: DateTime.UtcNow.AddHours(-1)));

        HttpResponseMessage response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_ShouldReturnForbidden_WhenTokenHasNoSubjectClaim()
    {
        using TestApiFactory factory = new(new ECDsaSecurityKey(_signingAlgorithm));
        using HttpClient client = factory.CreateHttpsClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateToken(_signingAlgorithm, subject: null));

        HttpResponseMessage response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static string CreateToken(
        ECDsa signingAlgorithm,
        string? subject,
        DateTime? notBefore = null,
        DateTime? expires = null)
    {
        SigningCredentials credentials = new(
            new ECDsaSecurityKey(signingAlgorithm),
            SecurityAlgorithms.EcdsaSha256);

        List<Claim> claims = new();
        if (subject is not null)
        {
            claims.Add(new Claim("sub", subject));
        }

        JwtSecurityToken token = new(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            notBefore: notBefore ?? DateTime.UtcNow.AddMinutes(-1),
            expires: expires ?? DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class TestApiFactory : WebApplicationFactory<Program>
    {
        private readonly SecurityKey _validationKey;

        public TestApiFactory(SecurityKey validationKey) => _validationKey = validationKey;

        public HttpClient CreateHttpsClient() =>
            CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["SupabaseAuth:Issuer"] = Issuer,
                    ["SupabaseAuth:Audience"] = Audience
                });
            });
            builder.ConfigureTestServices(services =>
            {
                // No live JWKS endpoint in tests: validate against the in-test EC public key
                // directly instead of discovering it from Authority. Runs after the app's own
                // JwtBearer configuration, so it wins. The rest of the pipeline (real handler,
                // fallback authorization policy, /api/me, ICurrentUser) is exercised unchanged.
                services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.Authority = null;
                    options.TokenValidationParameters.IssuerSigningKey = _validationKey;
                });
            });
        }
    }
}
