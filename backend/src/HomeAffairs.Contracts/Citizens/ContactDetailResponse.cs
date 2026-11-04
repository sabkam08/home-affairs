namespace HomeAffairs.Contracts.Citizens;

public sealed record ContactDetailResponse(
    Guid ContactDetailId,
    Guid CitizenId,
    string ContactType,
    string ContactValue,
    bool IsPrimary,
    bool IsVerified,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    string RowVersion);
