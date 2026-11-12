using HomeAffairs.Domain.Documents;

namespace HomeAffairs.Application.Abstractions.Documents;

public interface IDocumentTextRepository
{
    Task<DocumentTextExtraction> SaveForApplicantAsync(
        Guid documentId,
        Guid applicationId,
        Guid userId,
        string extractionStatus,
        string? extractionMethod,
        string? extractedText,
        DateTimeOffset processedAtUtc,
        CancellationToken cancellationToken);

    Task<DocumentTextExtraction?>
        GetForApplicantAsync(
            Guid documentId,
            Guid applicationId,
            Guid userId,
            CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DocumentSearchResult>>
        SearchForApplicantAsync(
            Guid applicationId,
            Guid userId,
            string searchText,
            CancellationToken cancellationToken);
}
