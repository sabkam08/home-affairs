namespace HomeAffairs.Application.Abstractions.Identity;

public interface IRefreshTokenIssuer
{
    Task<IssuedRefreshToken> IssueAsync(
        Guid userId,
        CancellationToken cancellationToken);
}

public sealed record IssuedRefreshToken(
    string RefreshToken,
    DateTimeOffset ExpiresAtUtc);
