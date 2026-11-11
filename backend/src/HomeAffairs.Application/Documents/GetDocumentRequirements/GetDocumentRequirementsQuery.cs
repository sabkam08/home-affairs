using MediatR;

namespace HomeAffairs.Application.Documents.GetDocumentRequirements;

public sealed record GetDocumentRequirementsQuery(
    string ApplicationType)
    : IRequest<
        IReadOnlyCollection<
            DocumentRequirementResult>>;

public sealed record DocumentRequirementResult(
    string DocumentType,
    string DisplayName,
    string? Description,
    bool IsRequired,
    int MinimumCount);
