using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jadlify.API.Tests.Common;

/// <summary>
/// Deterministic test auth: the bearer token <em>is</em> the <c>sub</c> claim, so a request
/// authenticated as "user-a" is owner-scoped to that subject. The special tokens "invalid"
/// (auth failure → 401) and "missing-sub" (authenticated but no subject → 403) exercise the
/// global fallback policy without a real Supabase JWT.
/// </summary>
internal sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
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
