using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Application.Identity;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;
using Microsoft.Extensions.Options;

namespace HomeAffairs.Application.Identity.Login;

internal sealed class LoginCommandHandler(
    IUserRepository userRepository,
    ISecurityEventRepository securityEventRepository,
    IPasswordHasher passwordHasher,
    IAccessTokenProvider accessTokenProvider,
    IRequestContext requestContext,
    IClock clock,
    IRefreshTokenIssuer refreshTokenIssuer,
    IOptions<IdentitySecuritySettings> securitySettings)
    : IRequestHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var occurredAtUtc = clock.UtcNow;

        var normalizedEmail =
            request.Email.Trim().ToUpperInvariant();

        var user =
            await userRepository.GetByEmailAsync(
                normalizedEmail,
                cancellationToken);

        var refreshToken =
            await refreshTokenIssuer.IssueAsync(
                user.UserId,
                cancellationToken);

        if (user is null)
        {
            await RecordSecurityEventAsync(
                userId: null,
                eventType: SecurityEventTypes.LoginFailed,
                description:
                    "A login attempt failed for an unknown account.",
                occurredAtUtc,
                cancellationToken);

            throw new InvalidCredentialsException();
        }

        if (!user.IsActive)
        {
            await RecordSecurityEventAsync(
                user.UserId,
                SecurityEventTypes.LoginFailed,
                "A login attempt was made for an inactive account.",
                occurredAtUtc,
                cancellationToken);

            throw new InvalidCredentialsException();
        }

        if (user.IsLocked(occurredAtUtc))
        {
            await RecordSecurityEventAsync(
                user.UserId,
                SecurityEventTypes.LoginFailed,
                "A login attempt was made while the account was locked.",
                occurredAtUtc,
                cancellationToken);

            throw new InvalidCredentialsException();
        }

        if (!passwordHasher.Verify(
                request.Password,
                user.PasswordHash))
        {
            var attempt =
                await userRepository.RecordFailedLoginAsync(
                    user.UserId,
                    securitySettings.Value
                        .MaximumFailedLoginAttempts,
                    securitySettings.Value
                        .LockoutDurationMinutes,
                    occurredAtUtc,
                    cancellationToken);

            var accountWasLocked =
                attempt.LockedUntilUtc.HasValue
                && attempt.LockedUntilUtc.Value
                    > occurredAtUtc;

            await RecordSecurityEventAsync(
                user.UserId,
                accountWasLocked
                    ? SecurityEventTypes.AccountLocked
                    : SecurityEventTypes.LoginFailed,
                accountWasLocked
                    ? "The account was locked after repeated failed login attempts."
                    : "A login attempt failed because the supplied password was incorrect.",
                occurredAtUtc,
                cancellationToken);

            throw new InvalidCredentialsException();
        }

        await userRepository.RecordSuccessfulLoginAsync(
            user.UserId,
            occurredAtUtc,
            cancellationToken);

        var roles =
            await userRepository.GetRolesAsync(
                user.UserId,
                cancellationToken);

        var accessToken =
            accessTokenProvider.Create(user, roles);

        await RecordSecurityEventAsync(
            user.UserId,
            SecurityEventTypes.LoginSucceeded,
            "The user authenticated successfully.",
            occurredAtUtc,
            cancellationToken);

        return new LoginResult(
            user.UserId,
            user.Email,
            user.FirstName,
            user.LastName,
            roles,
            accessToken.AccessToken,
            accessToken.ExpiresAtUtc,
            refreshToken.RefreshToken,
            refreshToken.ExpiresAtUtc);
    }

    private Task RecordSecurityEventAsync(
        Guid? userId,
        string eventType,
        string description,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken)
    {
        var securityEvent = new SecurityEvent(
            Guid.NewGuid(),
            userId,
            eventType,
            description,
            requestContext.IpAddress,
            requestContext.UserAgent,
            requestContext.CorrelationId,
            occurredAtUtc);

        return securityEventRepository.CreateAsync(
            securityEvent,
            cancellationToken);
    }
}
