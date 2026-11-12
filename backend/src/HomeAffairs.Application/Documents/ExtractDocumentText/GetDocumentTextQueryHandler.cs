using HomeAffairs.Application.Abstractions.Documents;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;

namespace HomeAffairs.Application.Documents.ExtractDocumentText;

internal sealed class GetDocumentTextQueryHandler(
    IDocumentTextRepository repository,
    ICurrentUser currentUser)
    : IRequestHandler<
        GetDocumentTextQuery,
        DocumentTextResult>
{
    public async Task<DocumentTextResult> Handle(
        GetDocumentTextQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        var extraction =
            await repository.GetForApplicantAsync(
                request.DocumentId,
                request.ApplicationId,
                currentUser.UserId.Value,
                cancellationToken);

        if (extraction is null)
        {
            throw new DocumentNotFoundException();
        }

        return ExtractDocumentTextCommandHandler.Map(
            extraction);
    }
}
