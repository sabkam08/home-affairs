namespace HomeAffairs.Contracts.Documents;

public sealed record DocumentTextResponse(
    Guid DocumentId,
    Guid ApplicationId,
    string ExtractionStatus,
    string? ExtractionMethod,
    string? ExtractedText,
    int CharacterCount,
    DateTimeOffset? ProcessedAtUtc,
    string? ErrorMessage,
    string RowVersion);
