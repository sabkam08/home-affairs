using HomeAffairs.Application.Abstractions.Documents;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;

namespace HomeAffairs.Application.Documents.GetDocumentContent;

internal sealed class GetDocumentContentQueryHandler(
    IDocumentRepository repository,
    ICurrentUser currentUser)
    : IRequestHandler<
        GetDocumentContentQuery,
        DocumentContentResult>
{
    public async Task<DocumentContentResult> Handle(
        GetDocumentContentQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        var content =
            await repository
                .GetContentForApplicantAsync(
                    request.DocumentId,
                    request.ApplicationId,
                    currentUser.UserId.Value,
                    cancellationToken);

        if (content is null)
        {
            throw new DocumentNotFoundException();
        }

        return new DocumentContentResult(
            content.DocumentId,
            content.FileName,
            content.ContentType,
            content.FileSizeBytes,
            content.ContentHash,
            content.Content);
    }
}
