using Jadlify.SharedKernel;

namespace Jadlify.Application.Identity;

public sealed class UserScope
{
    private static readonly Error CrossUserAccessDenied = Error.Forbidden(
        "UserScope.Forbidden",
        "The requested resource does not belong to the current user.");

    private readonly ICurrentUser _currentUser;

    public UserScope(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public Result EnsureOwner(ApplicationUserId ownerId) =>
        ownerId == _currentUser.UserId
            ? Result.Ok()
            : Result.Fail(CrossUserAccessDenied);

    public Result<TValue> EnsureOwner<TValue>(ApplicationUserId ownerId, TValue value) =>
        ownerId == _currentUser.UserId
            ? Result.Ok(value)
            : Result.Fail<TValue>(CrossUserAccessDenied);
}
