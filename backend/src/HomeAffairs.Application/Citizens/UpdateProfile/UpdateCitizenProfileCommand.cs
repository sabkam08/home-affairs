using MediatR;

namespace HomeAffairs.Application.Citizens.UpdateProfile;

public sealed record UpdateCitizenProfileCommand(
    string? SouthAfricanIdNumber,
    string FirstName,
    string? MiddleNames,
    string LastName,
    DateOnly DateOfBirth,
    string Sex,
    byte[] ExpectedRowVersion)
    : IRequest<UpdateCitizenProfileResult>;

public sealed record UpdateCitizenProfileResult(
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
    byte[] RowVersion);
