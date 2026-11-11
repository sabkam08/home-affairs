using MediatR;

namespace HomeAffairs.Application.Documents.GetDocumentContent;

public sealed record GetDocumentContentQuery(
    Guid ApplicationId,
    Guid DocumentId)
    : IRequest<DocumentContentResult>;

public sealed record DocumentContentResult(
    Guid DocumentId,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string ContentHash,
    byte[] Content);
