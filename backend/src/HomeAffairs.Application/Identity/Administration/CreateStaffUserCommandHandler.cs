using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Domain.Common.Exceptions;
using HomeAffairs.Domain.Identity;
using MediatR;

namespace HomeAffairs.Application.Identity.Administration;

internal sealed class CreateStaffUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ISecurityEventRepository securityEventRepository,
    ICurrentUser currentUser,
    IRequestContext requestContext,
    IClock clock)
    : IRequestHandler<
        CreateStaffUserCommand,
        CreateStaffUserResult>
{
    public async Task<CreateStaffUserResult> Handle(
        CreateStaffUserCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

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

        var roleCode =
            request.RoleCode
                .Trim()
                .ToUpperInvariant();

        if (!Roles.StaffRoles.Contains(roleCode))
        {
            throw new DomainException(
                "The supplied staff role is not valid.");
        }

        var occurredAtUtc = clock.UtcNow;

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

        await userRepository.CreateStaffAsync(
            user,
            roleCode,
            currentUser.UserId.Value,
            cancellationToken);

        await securityEventRepository.CreateAsync(
            new SecurityEvent(
                Guid.NewGuid(),
                user.UserId,
                SecurityEventTypes.StaffUserCreated,
                $"A staff account was created with role {roleCode}.",
                requestContext.IpAddress,
                requestContext.UserAgent,
                requestContext.CorrelationId,
                occurredAtUtc),
            cancellationToken);

        return new CreateStaffUserResult(
            user.UserId,
            user.Email,
            user.FirstName,
            user.LastName,
            [roleCode]);
    }
}
