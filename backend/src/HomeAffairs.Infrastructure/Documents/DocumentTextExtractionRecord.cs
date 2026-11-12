using HomeAffairs.Domain.Documents;

namespace HomeAffairs.Infrastructure.Documents;

internal sealed class DocumentTextExtractionRecord
{
    public Guid DocumentTextExtractionId { get; init; }

    public Guid DocumentId { get; init; }

    public Guid ApplicationId { get; init; }

    public string ExtractionStatus { get; init; } =
        string.Empty;

    public string? ExtractionMethod { get; init; }

    public string? ExtractedText { get; init; }

    public int CharacterCount { get; init; }

    public DateTime? ProcessedAtUtc { get; init; }

    public string? ErrorMessage { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public byte[] RowVersion { get; init; } = [];

    public DocumentTextExtraction ToDomain()
    {
        return new DocumentTextExtraction
        {
            DocumentTextExtractionId =
                DocumentTextExtractionId,

            DocumentId =
                DocumentId,

            ApplicationId =
                ApplicationId,

            ExtractionStatus =
                ExtractionStatus,

            ExtractionMethod =
                ExtractionMethod,

            ExtractedText =
                ExtractedText,

            CharacterCount =
                CharacterCount,

            ProcessedAtUtc =
                ProcessedAtUtc.HasValue
                    ? ToUtc(
                        ProcessedAtUtc.Value)
                    : null,

            ErrorMessage =
                ErrorMessage,

            CreatedAtUtc =
                ToUtc(
                    CreatedAtUtc),

            UpdatedAtUtc =
                UpdatedAtUtc.HasValue
                    ? ToUtc(
                        UpdatedAtUtc.Value)
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
