using MediatR;

namespace HomeAffairs.Application.Citizens.CreateProfile;

public sealed record CreateCitizenProfileCommand(
    string? SouthAfricanIdNumber,
    string FirstName,
    string? MiddleNames,
    string LastName,
    DateOnly DateOfBirth,
    string Sex)
    : IRequest<CreateCitizenProfileResult>;

public sealed record CreateCitizenProfileResult(
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
