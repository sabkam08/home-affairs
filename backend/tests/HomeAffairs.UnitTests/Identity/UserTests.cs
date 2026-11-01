using FluentAssertions;
using HomeAffairs.Domain.Identity;

namespace HomeAffairs.UnitTests.Identity;

public sealed class UserTests
{
    [Fact]
    public void User_should_be_locked_when_lock_expiry_is_in_future()
    {
        var now =
            new DateTimeOffset(
                2026,
                10,
                31,
                12,
                0,
                0,
                TimeSpan.Zero);

        var user = new User
        {
            LockedUntilUtc =
                now.AddMinutes(15)
        };

        user.IsLocked(now)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void User_should_not_be_locked_when_lock_has_expired()
    {
        var now =
            new DateTimeOffset(
                2026,
                10,
                31,
                12,
                0,
                0,
                TimeSpan.Zero);

        var user = new User
        {
            LockedUntilUtc =
                now.AddMinutes(-1)
        };

        user.IsLocked(now)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void User_without_lock_should_not_be_locked()
    {
        var user = new User
        {
            LockedUntilUtc = null
        };

        user.IsLocked(DateTimeOffset.UtcNow)
            .Should()
            .BeFalse();
    }
}
