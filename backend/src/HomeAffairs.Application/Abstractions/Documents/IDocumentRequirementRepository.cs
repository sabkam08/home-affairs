using HomeAffairs.Domain.Documents;

namespace HomeAffairs.Application.Abstractions.Documents;

public interface IDocumentRequirementRepository
{
    Task<IReadOnlyCollection<DocumentRequirement>>
        GetByApplicationTypeAsync(
            string applicationType,
            CancellationToken cancellationToken);

    Task<IReadOnlyCollection<
        DocumentRequirementReadiness>>
        GetReadinessForApplicantAsync(
            Guid applicationId,
            Guid userId,
            CancellationToken cancellationToken);
}
