using MediatR;

namespace HomeAffairs.Application.Documents.SearchDocumentText;

public sealed record SearchDocumentTextQuery(
    Guid ApplicationId,
    string SearchText)
    : IRequest<
        IReadOnlyCollection<DocumentSearchResultModel>>;

public sealed record DocumentSearchResultModel(
    Guid DocumentId,
    Guid LogicalDocumentId,
    int VersionNumber,
    Guid ApplicationId,
    string DocumentType,
    string FileName,
    string ContentType,
    string FileExtension,
    long FileSizeBytes,
    string ContentHash,
    string ExtractionStatus,
    string? ExtractionMethod,
    int CharacterCount,
    DateTimeOffset? ProcessedAtUtc);
