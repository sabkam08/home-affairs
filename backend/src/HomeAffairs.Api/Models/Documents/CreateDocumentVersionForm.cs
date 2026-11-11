using Microsoft.AspNetCore.Http;

namespace HomeAffairs.Api.Models.Documents;

public sealed class CreateDocumentVersionForm
{
    public string RowVersion { get; init; } =
        string.Empty;

    public IFormFile File { get; init; } =
        null!;
}
