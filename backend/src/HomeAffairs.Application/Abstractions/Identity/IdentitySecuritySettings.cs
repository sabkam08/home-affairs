namespace HomeAffairs.Application.Abstractions.Identity;

public sealed class IdentitySecuritySettings
{
    public const string SectionName = "IdentitySecurity";

    public int MaximumFailedLoginAttempts { get; init; } = 5;

    public int LockoutDurationMinutes { get; init; } = 15;
}
