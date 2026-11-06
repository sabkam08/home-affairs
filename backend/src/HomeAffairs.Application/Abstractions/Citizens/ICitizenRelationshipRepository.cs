using HomeAffairs.Domain.Citizens;

namespace HomeAffairs.Application.Abstractions.Citizens;

public interface ICitizenRelationshipRepository
{
    Task<CitizenRelationship>
        CreateRepresentedMinorAsync(
            Citizen subject,
            Guid relationshipId,
            Guid representativeUserId,
            string relationshipType,
            CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CitizenRelationship>>
        GetByRepresentativeUserIdAsync(
            Guid representativeUserId,
            CancellationToken cancellationToken);

    Task<CitizenRelationship?>
        GetSubjectForRepresentativeAsync(
            Guid subjectCitizenId,
            Guid representativeUserId,
            CancellationToken cancellationToken);

    Task<CitizenRelationship>
        UpdateRelationshipAsync(
            Guid subjectCitizenId,
            Guid representativeUserId,
            string relationshipType,
            byte[] expectedRowVersion,
            DateTimeOffset updatedAtUtc,
            CancellationToken cancellationToken);

    Task DeactivateRelationshipAsync(
        Guid subjectCitizenId,
        Guid representativeUserId,
        byte[] expectedRowVersion,
        DateTimeOffset deactivatedAtUtc,
        CancellationToken cancellationToken);
}
