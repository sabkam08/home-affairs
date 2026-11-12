namespace HomeAffairs.Domain.Documents;

public sealed class DocumentTextExtraction
{
    public Guid DocumentTextExtractionId { get; init; }

    public Guid DocumentId { get; init; }

    public Guid ApplicationId { get; init; }

    public string ExtractionStatus { get; init; } =
        string.Empty;

    public string? ExtractionMethod { get; init; }

    public string? ExtractedText { get; init; }

    public int CharacterCount { get; init; }

    public DateTimeOffset? ProcessedAtUtc { get; init; }

    public string? ErrorMessage { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? UpdatedAtUtc { get; init; }

    public byte[] RowVersion { get; init; } = [];

    public bool HasSearchableText =>
        !string.IsNullOrWhiteSpace(
            ExtractedText)
        &&
        (
            ExtractionStatus ==
                DocumentExtractionStatuses.Extracted
            ||
            ExtractionStatus ==
                DocumentExtractionStatuses.OcrCompleted
        );
}
