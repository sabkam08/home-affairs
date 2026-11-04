namespace HomeAffairs.Contracts.Citizens;

public sealed record CreateContactDetailRequest(
    string ContactType,
    string ContactValue,
    bool IsPrimary);
