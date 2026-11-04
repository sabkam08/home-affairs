using HomeAffairs.Domain.Citizens;

namespace HomeAffairs.Infrastructure.Citizens;

internal sealed class ContactDetailRecord
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

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public byte[] RowVersion { get; init; } = [];

    public ContactDetail ToDomain()
    {
        return new ContactDetail
        {
            ContactDetailId =
                ContactDetailId,

            CitizenId =
                CitizenId,

            ContactType =
                ContactType,

            ContactValue =
                ContactValue,

            IsPrimary =
                IsPrimary,

            IsVerified =
                IsVerified,

            IsActive =
                IsActive,

            CreatedAtUtc =
                ToUtc(CreatedAtUtc),

            UpdatedAtUtc =
                UpdatedAtUtc.HasValue
                    ? ToUtc(
                        UpdatedAtUtc.Value)
                    : null,

            RowVersion =
                RowVersion
        };
    }

    private static DateTimeOffset ToUtc(
        DateTime value)
    {
        return new DateTimeOffset(
            DateTime.SpecifyKind(
                value,
                DateTimeKind.Utc));
    }
}
