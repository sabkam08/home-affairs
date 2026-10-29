using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;

namespace HomeAffairs.Application.Identity.Login;

internal sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IAccessTokenProvider accessTokenProvider,
    IClock clock)
    : IRequestHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail =
            request.Email.Trim().ToUpperInvariant();

        var user =
            await userRepository.GetByEmailAsync(
                normalizedEmail,
                cancellationToken);

        if (user is null ||
            !passwordHasher.Verify(
                request.Password,
                user.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        if (!user.IsActive)
        {
            throw new DomainException(
                "This user account is not active.");
        }

        if (user.IsLocked(clock.UtcNow))
        {
            throw new DomainException(
                "This user account is temporarily locked.");
        }

        var roles =
            await userRepository.GetRolesAsync(
                user.UserId,
                cancellationToken);

        var accessToken =
            accessTokenProvider.Create(user, roles);

        return new LoginResult(
            user.UserId,
            user.Email,
            user.FirstName,
            user.LastName,
            roles,
            accessToken.AccessToken,
            accessToken.ExpiresAtUtc);
    }
}
