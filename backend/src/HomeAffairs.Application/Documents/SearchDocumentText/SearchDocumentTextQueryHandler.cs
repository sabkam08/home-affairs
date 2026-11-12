using HomeAffairs.Application.Abstractions.Documents;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;

namespace HomeAffairs.Application.Documents.SearchDocumentText;

internal sealed class SearchDocumentTextQueryHandler(
    IDocumentTextRepository repository,
    ICurrentUser currentUser)
    : IRequestHandler<
        SearchDocumentTextQuery,
        IReadOnlyCollection<
            DocumentSearchResultModel>>
{
    public async Task<
        IReadOnlyCollection<
            DocumentSearchResultModel>>
        Handle(
            SearchDocumentTextQuery request,
            CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        try
        {
            var results =
                await repository.SearchForApplicantAsync(
                    request.ApplicationId,
                    currentUser.UserId.Value,
                    request.SearchText.Trim(),
                    cancellationToken);

            return results
                .Select(result =>
                    new DocumentSearchResultModel(
                        result.DocumentId,
                        result.LogicalDocumentId,
                        result.VersionNumber,
                        result.ApplicationId,
                        result.DocumentType,
                        result.FileName,
                        result.ContentType,
                        result.FileExtension,
                        result.FileSizeBytes,
                        result.ContentHash,
                        result.ExtractionStatus,
                        result.ExtractionMethod,
                        result.CharacterCount,
                        result.ProcessedAtUtc))
                .ToArray();
        }
        catch (
            DocumentReadinessApplicationNotFoundPersistenceException)
        {
            throw new DocumentNotFoundException();
        }
    }
}
