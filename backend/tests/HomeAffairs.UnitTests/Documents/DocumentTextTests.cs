using FluentAssertions;
using HomeAffairs.Domain.Documents;

namespace HomeAffairs.UnitTests.Documents;

public sealed class DocumentTextTests
{
    [Fact]
    public void Extracted_document_with_text_should_be_searchable()
    {
        var extraction =
            new DocumentTextExtraction
            {
                ExtractionStatus =
                    DocumentExtractionStatuses.Extracted,

                ExtractedText =
                    "Home Affairs proof of address"
            };

        extraction.HasSearchableText
            .Should()
            .BeTrue();
    }

    [Fact]
    public void Ocr_required_document_should_not_be_searchable_yet()
    {
        var extraction =
            new DocumentTextExtraction
            {
                ExtractionStatus =
                    DocumentExtractionStatuses.OcrRequired,

                ExtractedText =
                    null
            };

        extraction.HasSearchableText
            .Should()
            .BeFalse();
    }

    [Fact]
    public void Ocr_completed_document_with_text_should_be_searchable()
    {
        var extraction =
            new DocumentTextExtraction
            {
                ExtractionStatus =
                    DocumentExtractionStatuses.OcrCompleted,

                ExtractedText =
                    "OCR extracted identity evidence"
            };

        extraction.HasSearchableText
            .Should()
            .BeTrue();
    }
}
