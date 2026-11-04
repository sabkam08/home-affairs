namespace HomeAffairs.Contracts.Citizens;

public sealed record UpdateContactDetailRequest(
    string ContactType,
    string ContactValue,
    bool IsPrimary,
    string RowVersion);
