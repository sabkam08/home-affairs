using MediatR;

namespace HomeAffairs.Application.Citizens.ContactDetails;

public sealed record GetMyContactDetailsQuery
    : IRequest<
        IReadOnlyCollection<
            GetMyContactDetailsResult>>;

public sealed record GetMyContactDetailsResult(
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
