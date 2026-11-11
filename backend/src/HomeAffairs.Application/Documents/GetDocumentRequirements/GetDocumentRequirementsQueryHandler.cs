using HomeAffairs.Application.Abstractions.Documents;
using MediatR;

namespace HomeAffairs.Application.Documents.GetDocumentRequirements;

internal sealed class GetDocumentRequirementsQueryHandler(
    IDocumentRequirementRepository repository)
    : IRequestHandler<
        GetDocumentRequirementsQuery,
        IReadOnlyCollection<
            DocumentRequirementResult>>
{
    public async Task<
        IReadOnlyCollection<
            DocumentRequirementResult>>
        Handle(
            GetDocumentRequirementsQuery request,
            CancellationToken cancellationToken)
    {
        var requirements =
            await repository
                .GetByApplicationTypeAsync(
                    request.ApplicationType
                        .Trim()
                        .ToUpperInvariant(),
                    cancellationToken);

        return requirements
            .Select(requirement =>
                new DocumentRequirementResult(
                    requirement.DocumentType,
                    requirement.DisplayName,
                    requirement.Description,
                    requirement.IsRequired,
                    requirement.MinimumCount))
            .ToArray();
    }
}
