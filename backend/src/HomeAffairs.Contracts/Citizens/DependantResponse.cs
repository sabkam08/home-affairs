namespace HomeAffairs.Contracts.Citizens;

public sealed record DependantResponse(
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
    string RelationshipRowVersion,
    string SubjectRowVersion);
