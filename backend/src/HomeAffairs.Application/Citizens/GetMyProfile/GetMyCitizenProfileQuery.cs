using MediatR;

namespace HomeAffairs.Application.Citizens.GetMyProfile;

public sealed record GetMyCitizenProfileQuery
    : IRequest<GetMyCitizenProfileResult>;

public sealed record GetMyCitizenProfileResult(
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
