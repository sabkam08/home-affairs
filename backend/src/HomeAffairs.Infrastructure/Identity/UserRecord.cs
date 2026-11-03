using HomeAffairs.Domain.Identity;

namespace HomeAffairs.Infrastructure.Identity;

internal sealed class UserRecord
{
    public Guid UserId { get; init; }

    public string Email { get; init; } =
        string.Empty;

    public string NormalizedEmail { get; init; } =
        string.Empty;

    public string PasswordHash { get; init; } =
        string.Empty;

    public string FirstName { get; init; } =
        string.Empty;

    public string LastName { get; init; } =
        string.Empty;

    public bool IsActive { get; init; }

    /*
        SQL DATETIME2 maps naturally to DateTime.
        We explicitly convert these values to
        DateTimeOffset UTC when creating the domain model.
    */
    public DateTime? EmailVerifiedAtUtc { get; init; }

    public int FailedLoginAttempts { get; init; }

    public DateTime? LockedUntilUtc { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public User ToDomain()
    {
        return new User
        {
            UserId = UserId,
            Email = Email,
            NormalizedEmail = NormalizedEmail,
            PasswordHash = PasswordHash,
            FirstName = FirstName,
            LastName = LastName,
            IsActive = IsActive,

            EmailVerifiedAtUtc =
                ToUtcDateTimeOffset(
                    EmailVerifiedAtUtc),

            FailedLoginAttempts =
                FailedLoginAttempts,

            LockedUntilUtc =
                ToUtcDateTimeOffset(
                    LockedUntilUtc),

            CreatedAtUtc =
                ToUtcDateTimeOffset(
                    CreatedAtUtc),

            UpdatedAtUtc =
                ToUtcDateTimeOffset(
                    UpdatedAtUtc)
        };
    }

    private static DateTimeOffset
        ToUtcDateTimeOffset(
            DateTime value)
    {
        return new DateTimeOffset(
            DateTime.SpecifyKind(
                value,
                DateTimeKind.Utc));
    }

    private static DateTimeOffset?
        ToUtcDateTimeOffset(
            DateTime? value)
    {
        return value.HasValue
            ? ToUtcDateTimeOffset(value.Value)
            : null;
    }
}
