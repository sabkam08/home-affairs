using HomeAffairs.Application.Abstractions.Documents;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Application.Documents.UploadDocument;
using HomeAffairs.Domain.Common.Exceptions;
using HomeAffairs.Domain.Documents;
using MediatR;

namespace HomeAffairs.Application.Documents.CreateDocumentVersion;

internal sealed class CreateDocumentVersionCommandHandler(
    IDocumentRepository documentRepository,
    IDocumentHashProvider hashProvider,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<
        CreateDocumentVersionCommand,
        DocumentResult>
{
    public async Task<DocumentResult> Handle(
        CreateDocumentVersionCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        var extension =
            Path.GetExtension(request.FileName);

        if (string.IsNullOrWhiteSpace(extension)
            ||
            !SupportedDocumentFormats
                .Extensions
                .Contains(extension))
        {
            throw new InvalidDocumentException(
                "The file extension is not supported.");
        }

        var newVersion =
            new Document
            {
                DocumentId =
                    Guid.NewGuid(),

                ApplicationId =
                    request.ApplicationId,

                FileName =
                    Path.GetFileName(
                        request.FileName.Trim()),

                ContentType =
                    request.ContentType
                        .Trim()
                        .ToLowerInvariant(),

                FileExtension =
                    extension
                        .ToLowerInvariant(),

                FileSizeBytes =
                    request.Content.LongLength,

                ContentHash =
                    hashProvider.ComputeSha256(
                        request.Content),

                UploadedByUserId =
                    currentUser.UserId.Value,

                IsActive =
                    true,

                CreatedAtUtc =
                    clock.UtcNow
            };

        try
        {
            var created =
                await documentRepository
                    .CreateVersionForApplicantAsync(
                        request.DocumentId,
                        newVersion,
                        request.Content,
                        currentUser.UserId.Value,
                        request.ExpectedRowVersion,
                        cancellationToken);

            return UploadDocumentCommandHandler.Map(
                created);
        }
        catch (DocumentNotFoundPersistenceException)
        {
            throw new DocumentNotFoundException();
        }
        catch (
            DuplicateDocumentContentPersistenceException)
        {
            throw new
                DuplicateDocumentContentException();
        }
        catch (DocumentConcurrencyPersistenceException)
        {
            throw new DocumentConflictException();
        }
    }
}
