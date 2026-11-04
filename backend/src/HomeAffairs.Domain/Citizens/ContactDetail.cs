namespace HomeAffairs.Domain.Citizens;

public sealed class ContactDetail
{
    public Guid ContactDetailId { get; init; }

    public Guid CitizenId { get; init; }

    public string ContactType { get; init; } =
        string.Empty;

    public string ContactValue { get; init; } =
        string.Empty;

    public bool IsPrimary { get; init; }

    public bool IsVerified { get; init; }

    public bool IsActive { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? UpdatedAtUtc { get; init; }

    public byte[] RowVersion { get; init; } = [];
}
