namespace HomeAffairs.Application.Abstractions.Documents;

public interface IDocumentTextExtractor
{
    bool CanExtract(
        string fileExtension);

    Task<DocumentTextExtractionData>
        ExtractAsync(
            string fileExtension,
            byte[] content,
            CancellationToken cancellationToken);
}

public sealed record DocumentTextExtractionData(
    string ExtractionStatus,
    string? ExtractionMethod,
    string? ExtractedText);
