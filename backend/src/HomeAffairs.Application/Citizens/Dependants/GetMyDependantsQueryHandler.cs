using HomeAffairs.Application.Abstractions.Citizens;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;

namespace HomeAffairs.Application.Citizens.Dependants;

internal sealed class GetMyDependantsQueryHandler(
    ICitizenRelationshipRepository relationshipRepository,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<
        GetMyDependantsQuery,
        IReadOnlyCollection<DependantResult>>
{
    public async Task<IReadOnlyCollection<DependantResult>>
        Handle(
            GetMyDependantsQuery request,
            CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        var relationships =
            await relationshipRepository
                .GetByRepresentativeUserIdAsync(
                    currentUser.UserId.Value,
                    cancellationToken);

        var today =
            DateOnly.FromDateTime(
                clock.UtcNow.UtcDateTime);

        return relationships
            .Select(relationship =>
                CreateDependantCommandHandler.Map(
                    relationship,
                    today))
            .ToArray();
    }
}
