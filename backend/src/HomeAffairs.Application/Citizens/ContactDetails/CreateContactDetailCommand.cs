using MediatR;

namespace HomeAffairs.Application.Citizens.ContactDetails;

public sealed record CreateContactDetailCommand(
    string ContactType,
    string ContactValue,
    bool IsPrimary)
    : IRequest<CreateContactDetailResult>;

public sealed record CreateContactDetailResult(
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
