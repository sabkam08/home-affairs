using HomeAffairs.Application.Abstractions.Documents;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;

namespace HomeAffairs.Application.Documents.DeactivateDocument;

internal sealed class DeactivateDocumentCommandHandler(
    IDocumentRepository documentRepository,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<DeactivateDocumentCommand>
{
    public async Task Handle(
        DeactivateDocumentCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        try
        {
            await documentRepository
                .DeactivateForApplicantAsync(
                    request.DocumentId,
                    request.ApplicationId,
                    currentUser.UserId.Value,
                    request.ExpectedRowVersion,
                    clock.UtcNow,
                    cancellationToken);
        }
        catch (DocumentNotFoundPersistenceException)
        {
            throw new DocumentNotFoundException();
        }
        catch (DocumentConcurrencyPersistenceException)
        {
            throw new DocumentConflictException();
        }
    }
}
