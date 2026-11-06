using HomeAffairs.Domain.Citizens;

namespace HomeAffairs.Infrastructure.Citizens;

internal sealed class CitizenRelationshipRecord
{
    public Guid CitizenRelationshipId { get; init; }

    public Guid RepresentativeCitizenId { get; init; }

    public Guid SubjectCitizenId { get; init; }

    public string RelationshipType { get; init; } =
        string.Empty;

    public bool RelationshipIsActive { get; init; }

    public DateTime RelationshipCreatedAtUtc { get; init; }

    public DateTime? RelationshipUpdatedAtUtc { get; init; }

    public byte[] RelationshipRowVersion { get; init; } =
        [];

    public Guid? SubjectUserId { get; init; }

    public string? SouthAfricanIdNumber { get; init; }

    public string FirstName { get; init; } =
        string.Empty;

    public string? MiddleNames { get; init; }

    public string LastName { get; init; } =
        string.Empty;

    public DateTime DateOfBirth { get; init; }

    public string Sex { get; init; } =
        string.Empty;

    public bool SubjectIsActive { get; init; }

    public DateTime SubjectCreatedAtUtc { get; init; }

    public DateTime? SubjectUpdatedAtUtc { get; init; }

    public byte[] SubjectRowVersion { get; init; } =
        [];

    public CitizenRelationship ToDomain()
    {
        return new CitizenRelationship
        {
            CitizenRelationshipId =
                CitizenRelationshipId,

            RepresentativeCitizenId =
                RepresentativeCitizenId,

            SubjectCitizenId =
                SubjectCitizenId,

            RelationshipType =
                RelationshipType,

            IsActive =
                RelationshipIsActive,

            CreatedAtUtc =
                ToUtc(
                    RelationshipCreatedAtUtc),

            UpdatedAtUtc =
                RelationshipUpdatedAtUtc.HasValue
                    ? ToUtc(
                        RelationshipUpdatedAtUtc.Value)
                    : null,

            RowVersion =
                RelationshipRowVersion,

            Subject =
                new Citizen
                {
                    CitizenId =
                        SubjectCitizenId,

                    UserId =
                        SubjectUserId,

                    SouthAfricanIdNumber =
                        SouthAfricanIdNumber,

                    FirstName =
                        FirstName,

                    MiddleNames =
                        MiddleNames,

                    LastName =
                        LastName,

                    DateOfBirth =
                        DateOnly.FromDateTime(
                            DateOfBirth),

                    Sex =
                        Sex,

                    IsActive =
                        SubjectIsActive,

                    CreatedAtUtc =
                        ToUtc(
                            SubjectCreatedAtUtc),

                    UpdatedAtUtc =
                        SubjectUpdatedAtUtc.HasValue
                            ? ToUtc(
                                SubjectUpdatedAtUtc.Value)
                            : null,

                    RowVersion =
                        SubjectRowVersion
                }
        };
    }

    private static DateTimeOffset ToUtc(
        DateTime value)
    {
        return new DateTimeOffset(
            DateTime.SpecifyKind(
                value,
                DateTimeKind.Utc));
    }
}
