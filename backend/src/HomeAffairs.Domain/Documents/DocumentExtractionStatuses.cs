namespace HomeAffairs.Domain.Documents;

public static class DocumentExtractionStatuses
{
    public const string NotProcessed =
        "NOT_PROCESSED";

    public const string Extracted =
        "EXTRACTED";

    public const string OcrRequired =
        "OCR_REQUIRED";

    public const string OcrCompleted =
        "OCR_COMPLETED";

    public const string Failed =
        "FAILED";
}
