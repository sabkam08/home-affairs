using HomeAffairs.Application.Abstractions.Documents;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Domain.Common.Exceptions;
using HomeAffairs.Domain.Documents;
using MediatR;

namespace HomeAffairs.Application.Documents.UploadDocument;

internal sealed class UploadDocumentCommandHandler(
    IDocumentRepository documentRepository,
    IDocumentHashProvider hashProvider,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<
        UploadDocumentCommand,
        DocumentResult>
{
    public async Task<DocumentResult> Handle(
        UploadDocumentCommand request,
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

        var documentId =
            Guid.NewGuid();

        var document =
            new Document
            {
                DocumentId =
                    documentId,

                LogicalDocumentId =
                    documentId,

                VersionNumber =
                    1,

                ApplicationId =
                    request.ApplicationId,

                DocumentType =
                    request.DocumentType
                        .Trim()
                        .ToUpperInvariant(),

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
                    .CreateForApplicantAsync(
                        document,
                        request.Content,
                        currentUser.UserId.Value,
                        cancellationToken);

            return Map(created);
        }
        catch (
            DocumentApplicationNotFoundPersistenceException)
        {
            throw new DocumentNotFoundException();
        }
    }

    internal static DocumentResult Map(
        Document document)
    {
        return new DocumentResult(
            document.DocumentId,
            document.LogicalDocumentId,
            document.VersionNumber,
            document.ApplicationId,
            document.DocumentType,
            document.FileName,
            document.ContentType,
            document.FileExtension,
            document.FileSizeBytes,
            document.ContentHash,
            document.UploadedByUserId,
            document.IsActive,
            document.CreatedAtUtc,
            document.UpdatedAtUtc,
            document.RowVersion);
    }
}
