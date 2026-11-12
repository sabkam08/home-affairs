using MediatR;

namespace HomeAffairs.Application.Documents.ExtractDocumentText;

public sealed record GetDocumentTextQuery(
    Guid ApplicationId,
    Guid DocumentId)
    : IRequest<DocumentTextResult>;
