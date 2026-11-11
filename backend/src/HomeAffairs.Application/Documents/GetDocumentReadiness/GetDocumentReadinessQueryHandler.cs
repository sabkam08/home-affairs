using HomeAffairs.Application.Abstractions.Documents;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;

namespace HomeAffairs.Application.Documents.GetDocumentReadiness;

internal sealed class GetDocumentReadinessQueryHandler(
    IDocumentRequirementRepository repository,
    ICurrentUser currentUser)
    : IRequestHandler<
        GetDocumentReadinessQuery,
        DocumentReadinessResult>
{
    public async Task<DocumentReadinessResult> Handle(
        GetDocumentReadinessQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        try
        {
            var requirements =
                await repository
                    .GetReadinessForApplicantAsync(
                        request.ApplicationId,
                        currentUser.UserId.Value,
                        cancellationToken);

            var items =
                requirements
                    .Select(requirement =>
                        new DocumentRequirementStatusResult(
                            requirement.DocumentType,
                            requirement.DisplayName,
                            requirement.Description,
                            requirement.MinimumCount,
                            requirement.UploadedCount,
                            requirement.IsSatisfied))
                    .ToArray();

            var requiredCount =
                items.Length;

            var satisfiedCount =
                items.Count(item =>
                    item.IsSatisfied);

            return new DocumentReadinessResult(
                request.ApplicationId,
                requiredCount ==
                    satisfiedCount,
                requiredCount,
                satisfiedCount,
                items);
        }
        catch (
            DocumentReadinessApplicationNotFoundPersistenceException)
        {
            throw new DocumentNotFoundException();
        }
    }
}
