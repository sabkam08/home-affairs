namespace HomeAffairs.Contracts.Citizens;

public sealed record CreateDependantRequest(
    string? SouthAfricanIdNumber,
    string FirstName,
    string? MiddleNames,
    string LastName,
    DateOnly DateOfBirth,
    string Sex,
    string RelationshipType);
