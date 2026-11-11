using HomeAffairs.Application.Abstractions.Documents;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Documents.UploadDocument;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;

namespace HomeAffairs.Application.Documents.GetApplicationDocuments;

internal sealed class GetApplicationDocumentsQueryHandler(
    IDocumentRepository repository,
    ICurrentUser currentUser)
    : IRequestHandler<
        GetApplicationDocumentsQuery,
        IReadOnlyCollection<DocumentResult>>
{
    public async Task<
        IReadOnlyCollection<DocumentResult>>
        Handle(
            GetApplicationDocumentsQuery request,
            CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        try
        {
            var documents =
                await repository
                    .GetByApplicationForApplicantAsync(
                        request.ApplicationId,
                        currentUser.UserId.Value,
                        cancellationToken);

            return documents
                .Select(
                    UploadDocumentCommandHandler.Map)
                .ToArray();
        }
        catch (
            DocumentApplicationNotFoundPersistenceException)
        {
            throw new DocumentNotFoundException();
        }
    }
}
