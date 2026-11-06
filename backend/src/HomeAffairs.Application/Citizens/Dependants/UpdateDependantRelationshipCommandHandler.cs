using HomeAffairs.Application.Abstractions.Citizens;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;

namespace HomeAffairs.Application.Citizens.Dependants;

internal sealed class
    UpdateDependantRelationshipCommandHandler(
        ICitizenRelationshipRepository
            relationshipRepository,
        ICurrentUser currentUser,
        IClock clock)
    : IRequestHandler<
        UpdateDependantRelationshipCommand,
        DependantResult>
{
    public async Task<DependantResult> Handle(
        UpdateDependantRelationshipCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        try
        {
            var relationship =
                await relationshipRepository
                    .UpdateRelationshipAsync(
                        request.SubjectCitizenId,
                        currentUser.UserId.Value,
                        request.RelationshipType
                            .Trim()
                            .ToUpperInvariant(),
                        request.ExpectedRowVersion,
                        clock.UtcNow,
                        cancellationToken);

            var today =
                DateOnly.FromDateTime(
                    clock.UtcNow.UtcDateTime);

            return CreateDependantCommandHandler.Map(
                relationship,
                today);
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
