using MediatR;

namespace HomeAffairs.Application.Documents.GetDocumentReadiness;

public sealed record GetDocumentReadinessQuery(
    Guid ApplicationId)
    : IRequest<DocumentReadinessResult>;

public sealed record DocumentReadinessResult(
    Guid ApplicationId,
    bool IsReady,
    int RequiredCount,
    int SatisfiedCount,
    IReadOnlyCollection<
        DocumentRequirementStatusResult> Requirements);

public sealed record DocumentRequirementStatusResult(
    string DocumentType,
    string DisplayName,
    string? Description,
    int MinimumCount,
    int UploadedCount,
    bool IsSatisfied);
