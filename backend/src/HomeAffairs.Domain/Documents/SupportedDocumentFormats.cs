namespace HomeAffairs.Domain.Documents;

public static class SupportedDocumentFormats
{
    public const long MaximumFileSizeBytes =
        15 * 1024 * 1024;

    public static readonly IReadOnlySet<string>
        Extensions =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase)
            {
                ".pdf",
                ".docx",
                ".jpg",
                ".jpeg",
                ".png"
            };

    public static readonly IReadOnlySet<string>
        ContentTypes =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase)
            {
                "application/pdf",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "image/jpeg",
                "image/png"
            };
}
