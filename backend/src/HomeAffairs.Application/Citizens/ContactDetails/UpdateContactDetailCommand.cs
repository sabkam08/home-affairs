using MediatR;

namespace HomeAffairs.Application.Citizens.ContactDetails;

public sealed record UpdateContactDetailCommand(
    Guid ContactDetailId,
    string ContactType,
    string ContactValue,
    bool IsPrimary,
    byte[] ExpectedRowVersion)
    : IRequest<UpdateContactDetailResult>;

public sealed record UpdateContactDetailResult(
    Guid ContactDetailId,
    Guid CitizenId,
    string ContactType,
    string ContactValue,
    bool IsPrimary,
    bool IsVerified,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    byte[] RowVersion);
