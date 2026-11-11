namespace HomeAffairs.Domain.Documents;

public sealed class DocumentRequirement
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

    public bool IsActive { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? UpdatedAtUtc { get; init; }

    public byte[] RowVersion { get; init; } = [];
}
