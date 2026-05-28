using System.Security.Claims;
using Jadlify.Application.Identity;
using Microsoft.Extensions.Options;

namespace Jadlify.API.Authentication;

public sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SupabaseJwtOptions _options;

    public HttpContextCurrentUser(
        IHttpContextAccessor httpContextAccessor,
        IOptions<SupabaseJwtOptions> options)
    {
        _httpContextAccessor = httpContextAccessor;
        _options = options.Value;
    }

    public ApplicationUserId UserId
    {
        get
        {
            ClaimsPrincipal? user = _httpContextAccessor.HttpContext?.User;

            if (user?.Identity?.IsAuthenticated != true)
            {
                throw new InvalidOperationException("An authenticated user is required.");
            }

            string? subject = user.FindFirstValue(_options.RequiredSubjectClaimName);

            if (string.IsNullOrWhiteSpace(subject))
            {
                throw new InvalidOperationException(
                    $"Authenticated user is missing the required '{_options.RequiredSubjectClaimName}' claim.");
            }

            return new ApplicationUserId(subject);
        }
    }
}
