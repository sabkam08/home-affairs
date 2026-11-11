using HomeAffairs.Domain.Documents;

namespace HomeAffairs.Infrastructure.Documents;

internal sealed class DocumentRequirementReadinessRecord
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

    public DocumentRequirementReadiness ToDomain()
    {
        return new DocumentRequirementReadiness
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

            UploadedCount =
                UploadedCount,

            IsSatisfied =
                IsSatisfied
        };
    }
}
