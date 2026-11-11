using HomeAffairs.Application.Abstractions.Documents;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Documents.UploadDocument;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;

namespace HomeAffairs.Application.Documents.GetDocumentVersions;

internal sealed class GetDocumentVersionsQueryHandler(
    IDocumentRepository documentRepository,
    ICurrentUser currentUser)
    : IRequestHandler<
        GetDocumentVersionsQuery,
        IReadOnlyCollection<DocumentResult>>
{
    public async Task<IReadOnlyCollection<DocumentResult>>
        Handle(
            GetDocumentVersionsQuery request,
            CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        try
        {
            var versions =
                await documentRepository
                    .GetVersionHistoryForApplicantAsync(
                        request.DocumentId,
                        request.ApplicationId,
                        currentUser.UserId.Value,
                        cancellationToken);

            return versions
                .Select(
                    UploadDocumentCommandHandler.Map)
                .ToArray();
        }
        catch (DocumentNotFoundPersistenceException)
        {
            throw new DocumentNotFoundException();
        }
    }
}
