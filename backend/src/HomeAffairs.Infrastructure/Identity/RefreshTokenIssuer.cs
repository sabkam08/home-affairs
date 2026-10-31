using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using Microsoft.Extensions.Options;

namespace HomeAffairs.Infrastructure.Identity;

internal sealed class RefreshTokenIssuer(
    IRefreshTokenGenerator tokenGenerator,
    IRefreshTokenRepository tokenRepository,
    IRequestContext requestContext,
    IClock clock,
    IOptions<JwtOptions> options)
    : IRefreshTokenIssuer
{
    private readonly JwtOptions _options = options.Value;

    public IssuedRefreshToken Create(Guid userId)
    {
        var generatedToken = tokenGenerator.Generate();

        var createdAtUtc = clock.UtcNow;

        var expiresAtUtc =
            createdAtUtc.AddDays(
                _options.RefreshTokenLifetimeDays);

        return new IssuedRefreshToken(
            Guid.NewGuid(),
            generatedToken.Token,
            generatedToken.TokenHash,
            createdAtUtc,
            expiresAtUtc);
    }

    public async Task<IssuedRefreshToken> IssueAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var issuedToken = Create(userId);

        var refreshTokenRecord =
            new RefreshTokenRecord(
                issuedToken.RefreshTokenId,
                userId,
                issuedToken.TokenHash,
                issuedToken.CreatedAtUtc,
                issuedToken.ExpiresAtUtc,
                requestContext.IpAddress);

        await tokenRepository.CreateAsync(
            refreshTokenRecord,
            cancellationToken);

        return issuedToken;
    }
}
