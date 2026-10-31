using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Application.Identity;
using MediatR;

namespace HomeAffairs.Application.Identity.Logout;

internal sealed class LogoutCommandHandler(
    IRefreshTokenGenerator tokenGenerator,
    IRefreshTokenRepository tokenRepository,
    ISecurityEventRepository securityEventRepository,
    IRequestContext requestContext,
    IClock clock)
    : IRequestHandler<LogoutCommand>
{
    public async Task Handle(
        LogoutCommand request,
        CancellationToken cancellationToken)
    {
        var occurredAtUtc = clock.UtcNow;

        var tokenHash =
            tokenGenerator.ComputeHash(
                request.RefreshToken);

        var result =
            await tokenRepository.RevokeAsync(
                tokenHash,
                occurredAtUtc,
                requestContext.IpAddress,
                cancellationToken);

        /*
            Logout is intentionally idempotent. An unknown or already
            revoked token still results in a successful HTTP response.
        */
        if (!result.TokenFound ||
            !result.UserId.HasValue)
        {
            return;
        }

        await securityEventRepository.CreateAsync(
            new SecurityEvent(
                Guid.NewGuid(),
                result.UserId,
                SecurityEventTypes.UserLoggedOut,
                "The user logged out and the refresh token was revoked.",
                requestContext.IpAddress,
                requestContext.UserAgent,
                requestContext.CorrelationId,
                occurredAtUtc),
            cancellationToken);
    }
}
