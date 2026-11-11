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
}
