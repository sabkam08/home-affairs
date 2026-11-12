using HomeAffairs.Domain.Documents;

namespace HomeAffairs.Infrastructure.Documents;

internal sealed class DocumentSearchRecord
{
    public Guid DocumentId { get; init; }

    public Guid LogicalDocumentId { get; init; }

    public int VersionNumber { get; init; }

    public Guid ApplicationId { get; init; }

    public string DocumentType { get; init; } =
        string.Empty;

    public string FileName { get; init; } =
        string.Empty;

    public string ContentType { get; init; } =
        string.Empty;

    public string FileExtension { get; init; } =
        string.Empty;

    public long FileSizeBytes { get; init; }

    public string ContentHash { get; init; } =
        string.Empty;

    public string ExtractionStatus { get; init; } =
        string.Empty;

    public string? ExtractionMethod { get; init; }

    public int CharacterCount { get; init; }

    public DateTime? ProcessedAtUtc { get; init; }

    public DocumentSearchResult ToDomain()
    {
        return new DocumentSearchResult
        {
            DocumentId =
                DocumentId,

            LogicalDocumentId =
                LogicalDocumentId,

            VersionNumber =
                VersionNumber,

            ApplicationId =
                ApplicationId,

            DocumentType =
                DocumentType,

            FileName =
                FileName,

            ContentType =
                ContentType,

            FileExtension =
                FileExtension,

            FileSizeBytes =
                FileSizeBytes,

            ContentHash =
                ContentHash,

            ExtractionStatus =
                ExtractionStatus,

            ExtractionMethod =
                ExtractionMethod,

            CharacterCount =
                CharacterCount,

            ProcessedAtUtc =
                ProcessedAtUtc.HasValue
                    ? new DateTimeOffset(
                        DateTime.SpecifyKind(
                            ProcessedAtUtc.Value,
                            DateTimeKind.Utc))
                    : null
        };
    }
}
