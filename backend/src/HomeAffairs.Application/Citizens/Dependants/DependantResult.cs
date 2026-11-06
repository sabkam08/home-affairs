namespace HomeAffairs.Application.Citizens.Dependants;

public sealed record DependantResult(
    Guid CitizenRelationshipId,
    Guid RepresentativeCitizenId,
    Guid SubjectCitizenId,
    string RelationshipType,
    string? SouthAfricanIdNumber,
    string FirstName,
    string? MiddleNames,
    string LastName,
    DateOnly DateOfBirth,
    string Sex,
    bool IsMinor,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    byte[] RelationshipRowVersion,
    byte[] SubjectRowVersion);
