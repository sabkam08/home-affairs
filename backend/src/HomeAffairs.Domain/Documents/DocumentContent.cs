namespace HomeAffairs.Domain.Documents;

public sealed class DocumentContent
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
}
