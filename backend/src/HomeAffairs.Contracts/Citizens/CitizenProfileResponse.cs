namespace HomeAffairs.Contracts.Citizens;

public sealed record CitizenProfileResponse(
    Guid CitizenId,
    string? SouthAfricanIdNumber,
    string FirstName,
    string? MiddleNames,
    string LastName,
    DateOnly DateOfBirth,
    string Sex,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    string RowVersion);
