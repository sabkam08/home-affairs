using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Application.Identity;
using HomeAffairs.Domain.Common.Exceptions;
using HomeAffairs.Domain.Identity;
using MediatR;

namespace HomeAffairs.Application.Identity.Register;

internal sealed class RegisterCommandHandler(
    IUserRepository userRepository,
    ISecurityEventRepository securityEventRepository,
    IPasswordHasher passwordHasher,
    IAccessTokenProvider accessTokenProvider,
    IRequestContext requestContext,
    IClock clock,
    IRefreshTokenIssuer refreshTokenIssuer)
    : IRequestHandler<RegisterCommand, RegisterResult>
{
    public async Task<RegisterResult> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var normalizedEmail = email.ToUpperInvariant();
        var occurredAtUtc = clock.UtcNow;

        var existingUser =
            await userRepository.GetByEmailAsync(
                normalizedEmail,
                cancellationToken);

        if (existingUser is not null)
        {
            throw new DomainException(
                "A user with this email address already exists.");
        }

        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = normalizedEmail,
            PasswordHash =
                passwordHasher.Hash(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            IsActive = true,
            FailedLoginAttempts = 0,
            CreatedAtUtc = occurredAtUtc
        };

        var refreshToken =
            await refreshTokenIssuer.IssueAsync(
                user.UserId,
                cancellationToken);

        await userRepository.CreateCitizenAsync(
            user,
            cancellationToken);

        IReadOnlyCollection<string> roles =
            ["CITIZEN"];

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

        var accessToken =
            accessTokenProvider.Create(user, roles);

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
