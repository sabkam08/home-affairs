namespace HomeAffairs.Contracts.Citizens;

public sealed record UpdateCitizenProfileRequest(
    string? SouthAfricanIdNumber,
    string FirstName,
    string? MiddleNames,
    string LastName,
    DateOnly DateOfBirth,
    string Sex,
    string RowVersion);
