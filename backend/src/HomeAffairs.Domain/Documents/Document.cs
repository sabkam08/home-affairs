namespace HomeAffairs.Domain.Documents;

public sealed class Document
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

    public Guid UploadedByUserId { get; init; }

    public bool IsActive { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? UpdatedAtUtc { get; init; }

    public byte[] RowVersion { get; init; } = [];
}
