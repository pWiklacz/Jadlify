using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Jadlify.API.Session;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jadlify.API.Tests.Session;

public class SessionEndpointTests
{
    [Fact]
    public async Task Me_ShouldReturnUnauthorized_WhenRequestIsAnonymous()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateHttpsClient();

        HttpResponseMessage response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_ShouldReturnForbidden_WhenAuthenticatedUserHasNoSubjectClaim()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateHttpsClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "missing-sub");

        HttpResponseMessage response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Me_ShouldReturnSubject_WhenAuthenticatedUserHasSubjectClaim()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateHttpsClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "user-123");

        HttpResponseMessage response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        MeResponse? body = await response.Content.ReadFromJsonAsync<MeResponse>();
        Assert.NotNull(body);
        Assert.Equal("user-123", body.UserId);
    }

    private sealed class TestApiFactory : WebApplicationFactory<Program>
    {
        public HttpClient CreateHttpsClient() =>
            CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(services =>
            {
                services
                    .AddAuthentication(TestAuthenticationHandler.AuthenticationScheme)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                        TestAuthenticationHandler.AuthenticationScheme,
                        _ => { });
            });
        }
    }

    private sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string AuthenticationScheme = "Test";

        public TestAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string? authorization = Request.Headers.Authorization;

            if (string.IsNullOrWhiteSpace(authorization))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            const string bearerPrefix = "Bearer ";
            if (!authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(AuthenticateResult.Fail("Unsupported authorization scheme."));
            }

            string token = authorization[bearerPrefix.Length..];

            if (token.Equals("invalid", StringComparison.Ordinal))
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid test token."));
            }

            Claim[] claims = token.Equals("missing-sub", StringComparison.Ordinal)
                ? Array.Empty<Claim>()
                : new[] { new Claim("sub", token) };

            ClaimsIdentity identity = new(claims, AuthenticationScheme);
            ClaimsPrincipal principal = new(identity);
            AuthenticationTicket ticket = new(principal, AuthenticationScheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
