namespace HomeAffairs.Application.Abstractions.Identity;

public interface IRefreshTokenIssuer
{
    IssuedRefreshToken Create(Guid userId);

    Task<IssuedRefreshToken> IssueAsync(
        Guid userId,
        CancellationToken cancellationToken);
}

public sealed record IssuedRefreshToken(
    Guid RefreshTokenId,
    string RefreshToken,
    byte[] TokenHash,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc);
