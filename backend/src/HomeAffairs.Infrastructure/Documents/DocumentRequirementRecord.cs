using HomeAffairs.Domain.Documents;

namespace HomeAffairs.Infrastructure.Documents;

internal sealed class DocumentRequirementRecord
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

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public byte[] RowVersion { get; init; } = [];

    public DocumentRequirement ToDomain()
    {
        return new DocumentRequirement
        {
            DocumentRequirementId =
                DocumentRequirementId,

            ApplicationType =
                ApplicationType,

            DocumentType =
                DocumentType,

            DisplayName =
                DisplayName,

            Description =
                Description,

            IsRequired =
                IsRequired,

            MinimumCount =
                MinimumCount,

            IsActive =
                IsActive,

            CreatedAtUtc =
                ToUtc(CreatedAtUtc),

            UpdatedAtUtc =
                UpdatedAtUtc.HasValue
                    ? ToUtc(UpdatedAtUtc.Value)
                    : null,

            RowVersion =
                RowVersion
        };
    }

    private static DateTimeOffset ToUtc(
        DateTime value)
    {
        return new DateTimeOffset(
            DateTime.SpecifyKind(
                value,
                DateTimeKind.Utc));
    }
}
