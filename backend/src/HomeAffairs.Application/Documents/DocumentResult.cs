namespace HomeAffairs.Application.Documents;

public sealed record DocumentResult(
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
    Guid UploadedByUserId,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    byte[] RowVersion);
