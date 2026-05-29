using Jadlify.Application.Identity;

namespace Jadlify.Infrastructure.Tests.Persistence;

internal sealed class TestCurrentUser : ICurrentUser
{
    public TestCurrentUser(ApplicationUserId userId)
    {
        UserId = userId;
    }

    public ApplicationUserId UserId { get; }
}
