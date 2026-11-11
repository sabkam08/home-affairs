using Microsoft.AspNetCore.Http;

namespace HomeAffairs.Api.Models.Documents;

public sealed class UploadDocumentForm
{
    public string DocumentType { get; init; } =
        string.Empty;

    public IFormFile File { get; init; } =
        null!;
}
