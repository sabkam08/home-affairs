using HomeAffairs.Domain.Documents;

namespace HomeAffairs.Application.Abstractions.Documents;

public interface IDocumentRepository
{
    Task<Document> CreateForApplicantAsync(
        Document document,
        byte[] content,
        Guid userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Document>>
        GetByApplicationForApplicantAsync(
            Guid applicationId,
            Guid userId,
            CancellationToken cancellationToken);

    Task<Document?> GetByIdForApplicantAsync(
        Guid documentId,
        Guid applicationId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<DocumentContent?>
        GetContentForApplicantAsync(
            Guid documentId,
            Guid applicationId,
            Guid userId,
            CancellationToken cancellationToken);

    Task<Document> CreateVersionForApplicantAsync(
        Guid documentId,
        Document newVersion,
        byte[] content,
        Guid userId,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Document>>
        GetVersionHistoryForApplicantAsync(
            Guid documentId,
            Guid applicationId,
            Guid userId,
            CancellationToken cancellationToken);

    Task DeactivateForApplicantAsync(
        Guid documentId,
        Guid applicationId,
        Guid userId,
        byte[] expectedRowVersion,
        DateTimeOffset deactivatedAtUtc,
        CancellationToken cancellationToken);
}
