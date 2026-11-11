using HomeAffairs.Domain.Documents;

namespace HomeAffairs.Infrastructure.Documents;

internal sealed class DocumentRecord
{
    public Guid DocumentId { get; init; }

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

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public byte[] RowVersion { get; init; } = [];

    public Document ToDomain()
    {
        return new Document
        {
            DocumentId =
                DocumentId,

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

            UploadedByUserId =
                UploadedByUserId,

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
