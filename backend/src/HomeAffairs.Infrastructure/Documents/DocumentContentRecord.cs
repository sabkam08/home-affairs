using HomeAffairs.Domain.Documents;

namespace HomeAffairs.Infrastructure.Documents;

internal sealed class DocumentContentRecord
{
    public Guid DocumentId { get; init; }

    public string FileName { get; init; } =
        string.Empty;

    public string ContentType { get; init; } =
        string.Empty;

    public long FileSizeBytes { get; init; }

    public string ContentHash { get; init; } =
        string.Empty;

    public byte[] Content { get; init; } = [];

    public DocumentContent ToDomain()
    {
        return new DocumentContent
        {
            DocumentId =
                DocumentId,

            FileName =
                FileName,

            ContentType =
                ContentType,

            FileSizeBytes =
                FileSizeBytes,

            ContentHash =
                ContentHash,

            Content =
                Content
        };
    }
}
