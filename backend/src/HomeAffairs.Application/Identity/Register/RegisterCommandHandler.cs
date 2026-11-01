using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Domain.Common.Exceptions;
using HomeAffairs.Domain.Identity;
using MediatR;

namespace HomeAffairs.Application.Identity.Register;

internal sealed class RegisterCommandHandler(
    IUserRepository userRepository,
    ISecurityEventRepository securityEventRepository,
    IPasswordHasher passwordHasher,
    IAccessTokenProvider accessTokenProvider,
    IRefreshTokenIssuer refreshTokenIssuer,
    IRequestContext requestContext,
    IClock clock)
    : IRequestHandler<RegisterCommand, RegisterResult>
{
    public async Task<RegisterResult> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();

        var normalizedEmail =
            email.ToUpperInvariant();

        var existingUser =
            await userRepository.GetByEmailAsync(
                normalizedEmail,
                cancellationToken);

        if (existingUser is not null)
        {
            throw new DomainException(
                "A user with this email address already exists.");
        }

        var occurredAtUtc = clock.UtcNow;

        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = normalizedEmail,
            PasswordHash =
                passwordHasher.Hash(request.Password),
            FirstName =
                request.FirstName.Trim(),
            LastName =
                request.LastName.Trim(),
            IsActive = true,
            FailedLoginAttempts = 0,
            CreatedAtUtc = occurredAtUtc
        };

        /*
            The database user must be created before any
            dependent records are written.

            [identity].RefreshTokens and
            [identity].SecurityEvents both reference
            [identity].Users through foreign keys.
        */
        await userRepository.CreateCitizenAsync(
            user,
            cancellationToken);

        /*
            Issue and persist the initial refresh token only
            after the user exists in [identity].Users.
        */
        var refreshToken =
            await refreshTokenIssuer.IssueAsync(
                user.UserId,
                cancellationToken);

        /*
            Persist the registration security event after
            the account and initial session are established.
        */
        await securityEventRepository.CreateAsync(
            new SecurityEvent(
                Guid.NewGuid(),
                user.UserId,
                SecurityEventTypes.UserRegistered,
                "A citizen account was registered.",
                requestContext.IpAddress,
                requestContext.UserAgent,
                requestContext.CorrelationId,
                occurredAtUtc),
            cancellationToken);

        IReadOnlyCollection<string> roles =
            [Roles.Citizen];

        var accessToken =
            accessTokenProvider.Create(
                user,
                roles);

        return new RegisterResult(
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
}
