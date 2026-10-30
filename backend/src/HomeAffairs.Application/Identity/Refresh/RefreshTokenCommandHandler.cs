using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Application.Identity;
using MediatR;
using Microsoft.Extensions.Options;

namespace HomeAffairs.Application.Identity.Refresh;

internal sealed class RefreshTokenCommandHandler(
    IRefreshTokenGenerator tokenGenerator,
    IRefreshTokenRepository tokenRepository,
    IUserRepository userRepository,
    IAccessTokenProvider accessTokenProvider,
    ISecurityEventRepository securityEventRepository,
    IRequestContext requestContext,
    IClock clock,
    IOptions<RefreshTokenSettings> settings)
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

        var generatedReplacement =
            tokenGenerator.Generate();

        var replacementExpiresAtUtc =
            occurredAtUtc.AddDays(
                settings.Value.RefreshTokenLifetimeDays);

        var replacement = new RefreshTokenRecord(
            Guid.NewGuid(),
            Guid.Empty,
            generatedReplacement.TokenHash,
            occurredAtUtc,
            replacementExpiresAtUtc,
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
                    rotation.UserId,
                    SecurityEventTypes
                        .RefreshTokenReuseDetected,
                    "A revoked refresh token was submitted again. " +
                    "All active refresh tokens were revoked.",
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
                cancellationToken)
            ?? throw new InvalidRefreshTokenException();

        if (!user.IsActive)
        {
            throw new InvalidRefreshTokenException();
        }

        var roles =
            await userRepository.GetRolesAsync(
                user.UserId,
                cancellationToken);

        var accessToken =
            accessTokenProvider.Create(user, roles);

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
            generatedReplacement.Token,
            replacementExpiresAtUtc);
    }

    private Task RecordEventAsync(
        Guid? userId,
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
