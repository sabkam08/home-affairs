using MediatR;

namespace HomeAffairs.Application.Documents.CreateDocumentVersion;

public sealed record CreateDocumentVersionCommand(
    Guid ApplicationId,
    Guid DocumentId,
    string FileName,
    string ContentType,
    byte[] Content,
    byte[] ExpectedRowVersion)
    : IRequest<DocumentResult>;
