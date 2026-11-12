using MediatR;

namespace HomeAffairs.Application.Documents.ExtractDocumentText;

public sealed record ExtractDocumentTextCommand(
    Guid ApplicationId,
    Guid DocumentId)
    : IRequest<DocumentTextResult>;

public sealed record DocumentTextResult(
    Guid DocumentId,
    Guid ApplicationId,
    string ExtractionStatus,
    string? ExtractionMethod,
    string? ExtractedText,
    int CharacterCount,
    DateTimeOffset? ProcessedAtUtc,
    string? ErrorMessage,
    byte[] RowVersion);
