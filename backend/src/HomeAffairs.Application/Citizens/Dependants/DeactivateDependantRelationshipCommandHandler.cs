using HomeAffairs.Application.Abstractions.Citizens;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;

namespace HomeAffairs.Application.Citizens.Dependants;

internal sealed class
    DeactivateDependantRelationshipCommandHandler(
        ICitizenRelationshipRepository
            relationshipRepository,
        ICurrentUser currentUser,
        IClock clock)
    : IRequestHandler<
        DeactivateDependantRelationshipCommand>
{
    public async Task Handle(
        DeactivateDependantRelationshipCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        try
        {
            await relationshipRepository
                .DeactivateRelationshipAsync(
                    request.SubjectCitizenId,
                    currentUser.UserId.Value,
                    request.ExpectedRowVersion,
                    clock.UtcNow,
                    cancellationToken);
        }
        catch (
            CitizenRelationshipNotFoundPersistenceException)
        {
            throw new DependantNotFoundException();
        }
        catch (
            CitizenRelationshipConcurrencyException)
        {
            throw new
                DependantRelationshipConflictException();
        }
    }
}
