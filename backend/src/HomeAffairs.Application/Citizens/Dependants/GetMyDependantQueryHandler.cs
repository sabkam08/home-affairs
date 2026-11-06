using HomeAffairs.Application.Abstractions.Citizens;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;

namespace HomeAffairs.Application.Citizens.Dependants;

internal sealed class GetMyDependantQueryHandler(
    ICitizenRelationshipRepository relationshipRepository,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<
        GetMyDependantQuery,
        DependantResult>
{
    public async Task<DependantResult> Handle(
        GetMyDependantQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        var relationship =
            await relationshipRepository
                .GetSubjectForRepresentativeAsync(
                    request.SubjectCitizenId,
                    currentUser.UserId.Value,
                    cancellationToken);

        if (relationship is null)
        {
            throw new DependantNotFoundException();
        }

        var today =
            DateOnly.FromDateTime(
                clock.UtcNow.UtcDateTime);

        return CreateDependantCommandHandler.Map(
            relationship,
            today);
    }
}
