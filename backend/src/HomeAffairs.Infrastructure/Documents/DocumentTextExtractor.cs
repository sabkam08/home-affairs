using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using HomeAffairs.Application.Abstractions.Documents;
using HomeAffairs.Domain.Documents;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace HomeAffairs.Infrastructure.Documents;

internal sealed class DocumentTextExtractor
    : IDocumentTextExtractor
{
    public bool CanExtract(
        string fileExtension)
    {
        return fileExtension.Equals(
                   ".docx",
                   StringComparison.OrdinalIgnoreCase)
               ||
               fileExtension.Equals(
                   ".pdf",
                   StringComparison.OrdinalIgnoreCase);
    }

    public Task<DocumentTextExtractionData>
        ExtractAsync(
            string fileExtension,
            byte[] content,
            CancellationToken cancellationToken)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        if (fileExtension.Equals(
                ".docx",
                StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(
                ExtractDocx(content));
        }

        if (fileExtension.Equals(
                ".pdf",
                StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(
                ExtractPdf(content));
        }

        /*
            Raster files need OCR.
            We persist this state rather than running
            native OCR synchronously inside the API.
        */
        return Task.FromResult(
            new DocumentTextExtractionData(
                DocumentExtractionStatuses.OcrRequired,
                null,
                null));
    }

    private static DocumentTextExtractionData
        ExtractDocx(
            byte[] content)
    {
        using var stream =
            new MemoryStream(content);

        using var document =
            WordprocessingDocument.Open(
                stream,
                false);

        var body =
            document.MainDocumentPart?
                .Document?
                .Body;

        var text =
            body is null
                ? string.Empty
                : string.Join(
                    Environment.NewLine,
                    body.Descendants<Paragraph>()
                        .Select(paragraph =>
                            paragraph.InnerText)
                        .Where(value =>
                            !string.IsNullOrWhiteSpace(
                                value)));

        return new DocumentTextExtractionData(
            DocumentExtractionStatuses.Extracted,
            DocumentExtractionMethods.Docx,
            Normalize(text));
    }

    private static DocumentTextExtractionData
        ExtractPdf(
            byte[] content)
    {
        using var stream =
            new MemoryStream(content);

        using var document =
            PdfDocument.Open(stream);

        var pages =
            document
                .GetPages()
                .Select(page =>
                    ContentOrderTextExtractor
                        .GetText(page));

        var text =
            string.Join(
                Environment.NewLine,
                pages);

        var normalized =
            Normalize(text);

        /*
            A scanned PDF can contain no extractable
            text. It therefore needs OCR just like an image.
        */
        if (string.IsNullOrWhiteSpace(
                normalized))
        {
            return new DocumentTextExtractionData(
                DocumentExtractionStatuses.OcrRequired,
                null,
                null);
        }

        return new DocumentTextExtractionData(
            DocumentExtractionStatuses.Extracted,
            DocumentExtractionMethods.Pdf,
            normalized);
    }

    private static string Normalize(
        string value)
    {
        return value
            .Replace("\0", string.Empty)
            .Trim();
    }
}
