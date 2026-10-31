using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using MediatR;

namespace HomeAffairs.Application.Identity.Refresh;

internal sealed class RefreshTokenCommandHandler(
    IRefreshTokenGenerator tokenGenerator,
    IRefreshTokenRepository tokenRepository,
    IRefreshTokenIssuer refreshTokenIssuer,
    IUserRepository userRepository,
    IAccessTokenProvider accessTokenProvider,
    ISecurityEventRepository securityEventRepository,
    IRequestContext requestContext,
    IClock clock)
    : IRequestHandler<
        RefreshTokenCommand,
        RefreshTokenResult>
{
    public async Task<RefreshTokenResult> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var occurredAtUtc = clock.UtcNow;

        var currentTokenHash =
            tokenGenerator.ComputeHash(
                request.RefreshToken);

        /*
            Create the replacement token in memory.

            It is NOT persisted here.
            The rotation stored procedure will atomically:
              1. validate the old token,
              2. revoke it,
              3. create this replacement.
        */
        var issuedReplacement =
            refreshTokenIssuer.Create(Guid.Empty);

        var replacement =
            new RefreshTokenRecord(
                issuedReplacement.RefreshTokenId,
                Guid.Empty,
                issuedReplacement.TokenHash,
                issuedReplacement.CreatedAtUtc,
                issuedReplacement.ExpiresAtUtc,
                requestContext.IpAddress);

        var rotation =
            await tokenRepository.RotateAsync(
                currentTokenHash,
                replacement,
                requestContext.IpAddress,
                cancellationToken);

        if (string.Equals(
                rotation.Status,
                "REVOKED",
                StringComparison.Ordinal))
        {
            if (rotation.UserId.HasValue)
            {
                await RecordEventAsync(
                    rotation.UserId.Value,
                    SecurityEventTypes
                        .RefreshTokenReuseDetected,
                    "A revoked refresh token was submitted again. "
                    + "All active refresh tokens were revoked.",
                    occurredAtUtc,
                    cancellationToken);
            }

            throw new RefreshTokenReuseException();
        }

        if (!string.Equals(
                rotation.Status,
                "ROTATED",
                StringComparison.Ordinal)
            || !rotation.UserId.HasValue)
        {
            throw new InvalidRefreshTokenException();
        }

        var user =
            await userRepository.GetByIdAsync(
                rotation.UserId.Value,
                cancellationToken);

        if (user is null || !user.IsActive)
        {
            throw new InvalidRefreshTokenException();
        }

        var roles =
            await userRepository.GetRolesAsync(
                user.UserId,
                cancellationToken);

        var accessToken =
            accessTokenProvider.Create(
                user,
                roles);

        await RecordEventAsync(
            user.UserId,
            SecurityEventTypes.RefreshTokenRotated,
            "A refresh token was rotated successfully.",
            occurredAtUtc,
            cancellationToken);

        return new RefreshTokenResult(
            user.UserId,
            user.Email,
            user.FirstName,
            user.LastName,
            roles,
            accessToken.AccessToken,
            accessToken.ExpiresAtUtc,
            issuedReplacement.RefreshToken,
            issuedReplacement.ExpiresAtUtc);
    }

    private Task RecordEventAsync(
        Guid userId,
        string eventType,
        string description,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken)
    {
        return securityEventRepository.CreateAsync(
            new SecurityEvent(
                Guid.NewGuid(),
                userId,
                eventType,
                description,
                requestContext.IpAddress,
                requestContext.UserAgent,
                requestContext.CorrelationId,
                occurredAtUtc),
            cancellationToken);
    }
}
