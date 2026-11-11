namespace HomeAffairs.Contracts.Documents;

public sealed record DocumentReadinessResponse(
    Guid ApplicationId,
    bool IsReady,
    int RequiredCount,
    int SatisfiedCount,
    IReadOnlyCollection<
        DocumentRequirementStatusResponse> Requirements);

public sealed record DocumentRequirementStatusResponse(
    string DocumentType,
    string DisplayName,
    string? Description,
    int MinimumCount,
    int UploadedCount,
    bool IsSatisfied);
