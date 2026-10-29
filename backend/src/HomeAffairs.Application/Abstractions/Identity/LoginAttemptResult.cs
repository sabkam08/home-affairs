namespace HomeAffairs.Application.Abstractions.Identity;

public sealed record LoginAttemptResult(
    int FailedLoginAttempts,
    DateTimeOffset? LockedUntilUtc);
