namespace HomeAffairs.Contracts.Citizens;

public sealed record CreateCitizenProfileRequest(
    string? SouthAfricanIdNumber,
    string FirstName,
    string? MiddleNames,
    string LastName,
    DateOnly DateOfBirth,
    string Sex);
