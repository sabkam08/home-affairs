namespace HomeAffairs.Domain.Citizens;

public sealed class CitizenRelationship
{
    public Guid CitizenRelationshipId { get; init; }

    public Guid RepresentativeCitizenId { get; init; }

    public Guid SubjectCitizenId { get; init; }

    public string RelationshipType { get; init; } =
        string.Empty;

    public bool IsActive { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? UpdatedAtUtc { get; init; }

    public byte[] RowVersion { get; init; } = [];

    public Citizen Subject { get; init; } =
        new();
}
