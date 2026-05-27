using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Jadlify.API.Authentication;
using Jadlify.Application.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jadlify.API.Tests.Authentication;

public class AuthBoundaryTests
{
    [Fact]
    public async Task ProtectedEndpoint_ShouldReturnUnauthorized_WhenRequestIsAnonymous()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateHttpsClient();

        HttpResponseMessage response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_ShouldReturnUnauthorized_WhenBearerTokenIsInvalid()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateHttpsClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid");

        HttpResponseMessage response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_ShouldReturnForbidden_WhenAuthenticatedUserHasNoSubjectClaim()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateHttpsClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "missing-sub");

        HttpResponseMessage response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_ShouldAllowRequest_WhenAuthenticatedUserHasSubjectClaim()
    {
        using TestApiFactory factory = new();
        using HttpClient client = factory.CreateHttpsClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "user-123");

        HttpResponseMessage response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void CurrentUser_ShouldExposeApplicationUserId_FromLiteralSubjectClaim()
    {
        DefaultHttpContext httpContext = new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim("sub", "user-123") },
                TestAuthenticationHandler.AuthenticationScheme))
        };
        HttpContextAccessor accessor = new()
        {
            HttpContext = httpContext
        };
        HttpContextCurrentUser currentUser = new(accessor, Options.Create(new SupabaseJwtOptions()));

        Assert.Equal(new ApplicationUserId("user-123"), currentUser.UserId);
    }

    private sealed class TestApiFactory : WebApplicationFactory<Program>
    {
        public HttpClient CreateHttpsClient() =>
            CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
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
