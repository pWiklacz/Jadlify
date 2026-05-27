using Jadlify.Application.Identity;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Tests.Identity;

public class UserScopeTests
{
    [Fact]
    public void EnsureOwner_ShouldReturnSuccess_WhenOwnerMatchesCurrentUser()
    {
        var userId = new ApplicationUserId("user-123");
        var userScope = new UserScope(new TestCurrentUser(userId));

        Result result = userScope.EnsureOwner(userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void EnsureOwner_ShouldReturnForbiddenFailure_WhenOwnerDiffersFromCurrentUser()
    {
        var userScope = new UserScope(new TestCurrentUser(new ApplicationUserId("current-user")));

        Result result = userScope.EnsureOwner(new ApplicationUserId("owner-user"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
        Assert.Equal("UserScope.Forbidden", result.Error.Code);
    }

    [Fact]
    public void EnsureOwner_ShouldReturnValue_WhenOwnerMatchesCurrentUser()
    {
        var userId = new ApplicationUserId("user-123");
        var userScope = new UserScope(new TestCurrentUser(userId));

        Result<string> result = userScope.EnsureOwner(userId, "allowed");

        Assert.True(result.IsSuccess);
        Assert.Equal("allowed", result.Value);
    }

    [Fact]
    public void EnsureOwner_ShouldReturnForbiddenValueFailure_WhenOwnerDiffersFromCurrentUser()
    {
        var userScope = new UserScope(new TestCurrentUser(new ApplicationUserId("current-user")));

        Result<string> result = userScope.EnsureOwner(new ApplicationUserId("owner-user"), "denied");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
        Assert.Equal("UserScope.Forbidden", result.Error.Code);
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public TestCurrentUser(ApplicationUserId userId)
        {
            UserId = userId;
        }

        public ApplicationUserId UserId { get; }
    }
}
