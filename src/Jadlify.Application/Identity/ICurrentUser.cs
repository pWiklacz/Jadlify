namespace Jadlify.Application.Identity;

public interface ICurrentUser
{
    ApplicationUserId UserId { get; }
}
