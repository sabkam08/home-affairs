namespace HomeAffairs.Contracts.Documents;

public sealed record DocumentRequirementResponse(
    string DocumentType,
    string DisplayName,
    string? Description,
    bool IsRequired,
    int MinimumCount);
