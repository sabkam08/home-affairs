using MediatR;

namespace HomeAffairs.Application.Documents.GetDocumentVersions;

public sealed record GetDocumentVersionsQuery(
    Guid ApplicationId,
    Guid DocumentId)
    : IRequest<IReadOnlyCollection<DocumentResult>>;
