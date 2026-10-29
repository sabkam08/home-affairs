namespace HomeAffairs.Domain.Identity;

public sealed class User
{
    public Guid UserId { get; init; }

    public string Email { get; init; } = string.Empty;

    public string NormalizedEmail { get; init; } = string.Empty;

    public string PasswordHash { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public DateTimeOffset? EmailVerifiedAtUtc { get; init; }

    public int FailedLoginAttempts { get; init; }

    public DateTimeOffset? LockedUntilUtc { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? UpdatedAtUtc { get; init; }

    public string FullName => $"{FirstName} {LastName}".Trim();

    public bool IsLocked(DateTimeOffset utcNow)
    {
        return LockedUntilUtc.HasValue
            && LockedUntilUtc.Value > utcNow;
    }
}
