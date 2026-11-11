using MediatR;

namespace HomeAffairs.Application.Documents.DeactivateDocument;

public sealed record DeactivateDocumentCommand(
    Guid ApplicationId,
    Guid DocumentId,
    byte[] ExpectedRowVersion)
    : IRequest;
