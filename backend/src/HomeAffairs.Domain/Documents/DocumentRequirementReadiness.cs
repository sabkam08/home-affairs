namespace HomeAffairs.Domain.Documents;

public sealed class DocumentRequirementReadiness
{
    public Guid DocumentRequirementId { get; init; }

    public string ApplicationType { get; init; } =
        string.Empty;

    public string DocumentType { get; init; } =
        string.Empty;

    public string DisplayName { get; init; } =
        string.Empty;

    public string? Description { get; init; }

    public bool IsRequired { get; init; }

    public int MinimumCount { get; init; }

    public int UploadedCount { get; init; }

    public bool IsSatisfied { get; init; }
}
