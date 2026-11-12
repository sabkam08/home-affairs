using HomeAffairs.Application.Abstractions.Documents;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;

namespace HomeAffairs.Application.Documents.ExtractDocumentText;

internal sealed class ExtractDocumentTextCommandHandler(
    IDocumentRepository documentRepository,
    IDocumentTextRepository textRepository,
    IDocumentTextExtractor extractor,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<
        ExtractDocumentTextCommand,
        DocumentTextResult>
{
    public async Task<DocumentTextResult> Handle(
        ExtractDocumentTextCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        var document =
            await documentRepository
                .GetByIdForApplicantAsync(
                    request.DocumentId,
                    request.ApplicationId,
                    currentUser.UserId.Value,
                    cancellationToken);

        if (document is null)
        {
            throw new DocumentNotFoundException();
        }

        var content =
            await documentRepository
                .GetContentForApplicantAsync(
                    request.DocumentId,
                    request.ApplicationId,
                    currentUser.UserId.Value,
                    cancellationToken);

        if (content is null)
        {
            throw new DocumentNotFoundException();
        }

        var extraction =
            await extractor.ExtractAsync(
                document.FileExtension,
                content.Content,
                cancellationToken);

        try
        {
            var saved =
                await textRepository.SaveForApplicantAsync(
                    request.DocumentId,
                    request.ApplicationId,
                    currentUser.UserId.Value,
                    extraction.ExtractionStatus,
                    extraction.ExtractionMethod,
                    extraction.ExtractedText,
                    clock.UtcNow,
                    cancellationToken);

            return Map(saved);
        }
        catch (DocumentNotFoundPersistenceException)
        {
            throw new DocumentNotFoundException();
        }
    }

    internal static DocumentTextResult Map(
        HomeAffairs.Domain.Documents
            .DocumentTextExtraction extraction)
    {
        return new DocumentTextResult(
            extraction.DocumentId,
            extraction.ApplicationId,
            extraction.ExtractionStatus,
            extraction.ExtractionMethod,
            extraction.ExtractedText,
            extraction.CharacterCount,
            extraction.ProcessedAtUtc,
            extraction.ErrorMessage,
            extraction.RowVersion);
    }
}
