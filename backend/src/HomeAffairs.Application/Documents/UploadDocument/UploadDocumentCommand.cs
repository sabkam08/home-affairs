using MediatR;

namespace HomeAffairs.Application.Documents.UploadDocument;

public sealed record UploadDocumentCommand(
    Guid ApplicationId,
    string DocumentType,
    string FileName,
    string ContentType,
    byte[] Content)
    : IRequest<DocumentResult>;
