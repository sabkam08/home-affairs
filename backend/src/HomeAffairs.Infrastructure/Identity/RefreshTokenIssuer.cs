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

    public async Task<IssuedRefreshToken> IssueAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var generatedToken = tokenGenerator.Generate();
        var createdAtUtc = clock.UtcNow;
        var expiresAtUtc =
            createdAtUtc.AddDays(
                _options.RefreshTokenLifetimeDays);

        var refreshToken = new RefreshTokenRecord(
            Guid.NewGuid(),
            userId,
            generatedToken.TokenHash,
            createdAtUtc,
            expiresAtUtc,
            requestContext.IpAddress);

        await tokenRepository.CreateAsync(
            refreshToken,
            cancellationToken);

        return new IssuedRefreshToken(
            generatedToken.Token,
            expiresAtUtc);
    }
}
