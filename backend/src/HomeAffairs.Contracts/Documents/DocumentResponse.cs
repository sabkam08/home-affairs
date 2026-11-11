namespace HomeAffairs.Contracts.Documents;

public sealed record DocumentResponse(
    Guid DocumentId,
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
    string RowVersion);
