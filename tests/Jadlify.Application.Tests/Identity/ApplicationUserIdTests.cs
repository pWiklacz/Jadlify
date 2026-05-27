using Jadlify.Application.Identity;

namespace Jadlify.Application.Tests.Identity;

public class ApplicationUserIdTests
{
    [Fact]
    public void Constructor_ShouldSetValue_WhenValueIsNotEmpty()
    {
        var userId = new ApplicationUserId("user-123");

        Assert.Equal("user-123", userId.Value);
        Assert.Equal("user-123", userId.ToString());
    }

    [Fact]
    public void Constructor_ShouldRejectInvalidValues()
    {
        Assert.Throws<ArgumentNullException>(() => new ApplicationUserId(null!));
        Assert.Throws<ArgumentException>(() => new ApplicationUserId(string.Empty));
        Assert.Throws<ArgumentException>(() => new ApplicationUserId("   "));
    }

    [Fact]
    public void Equals_ShouldCompareByValue()
    {
        var first = new ApplicationUserId("same-user");
        var second = new ApplicationUserId("same-user");
        var third = new ApplicationUserId("other-user");

        Assert.Equal(first, second);
        Assert.NotEqual(first, third);
    }
}
