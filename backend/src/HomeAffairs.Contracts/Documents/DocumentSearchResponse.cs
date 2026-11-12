namespace HomeAffairs.Contracts.Documents;

public sealed record DocumentSearchResponse(
    Guid DocumentId,
    Guid LogicalDocumentId,
    int VersionNumber,
    Guid ApplicationId,
    string DocumentType,
    string FileName,
    string ContentType,
    string FileExtension,
    long FileSizeBytes,
    string ContentHash,
    string ExtractionStatus,
    string? ExtractionMethod,
    int CharacterCount,
    DateTimeOffset? ProcessedAtUtc);
