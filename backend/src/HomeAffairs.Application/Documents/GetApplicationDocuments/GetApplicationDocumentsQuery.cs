using MediatR;

namespace HomeAffairs.Application.Documents.GetApplicationDocuments;

public sealed record GetApplicationDocumentsQuery(
    Guid ApplicationId)
    : IRequest<
        IReadOnlyCollection<DocumentResult>>;
