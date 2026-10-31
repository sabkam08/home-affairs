using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;

namespace HomeAffairs.Application.Identity.Administration;

internal sealed class RemoveRoleCommandHandler(
    IUserRepository userRepository,
    ISecurityEventRepository securityEventRepository,
    ICurrentUser currentUser,
    IRequestContext requestContext,
    IClock clock)
    : IRequestHandler<RemoveRoleCommand>
{
    public async Task Handle(
        RemoveRoleCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        var roleCode =
            request.RoleCode.Trim().ToUpperInvariant();

        var targetUser =
            await userRepository.GetByIdAsync(
                request.UserId,
                cancellationToken);

        if (targetUser is null)
        {
            throw new DomainException(
                "The target user could not be found.");
        }

        await userRepository.RemoveRoleAsync(
            request.UserId,
            roleCode,
            cancellationToken);

        await securityEventRepository.CreateAsync(
            new SecurityEvent(
                Guid.NewGuid(),
                request.UserId,
                SecurityEventTypes.UserRoleRemoved,
                $"Role {roleCode} was removed.",
                requestContext.IpAddress,
                requestContext.UserAgent,
                requestContext.CorrelationId,
                clock.UtcNow),
            cancellationToken);
    }
}
