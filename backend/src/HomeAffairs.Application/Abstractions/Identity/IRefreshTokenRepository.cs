namespace HomeAffairs.Application.Abstractions.Identity;

public interface IRefreshTokenRepository
{
    Task CreateAsync(
        RefreshTokenRecord refreshToken,
        CancellationToken cancellationToken);

    Task<RefreshTokenRotationResult> RotateAsync(
        byte[] currentTokenHash,
        RefreshTokenRecord replacementToken,
        string? ipAddress,
        CancellationToken cancellationToken);

    Task<RefreshTokenRevocationResult> RevokeAsync(
        byte[] tokenHash,
        DateTimeOffset revokedAtUtc,
        string? ipAddress,
        CancellationToken cancellationToken);

    Task<int> RevokeAllForUserAsync(
        Guid userId,
        DateTimeOffset revokedAtUtc,
        string? ipAddress,
        CancellationToken cancellationToken);
}

public sealed record RefreshTokenRecord(
    Guid RefreshTokenId,
    Guid UserId,
    byte[] TokenHash,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string? CreatedByIpAddress);

public sealed record RefreshTokenRotationResult(
    string Status,
    Guid? UserId);

public sealed record RefreshTokenRevocationResult(
    bool TokenFound,
    Guid? UserId);
