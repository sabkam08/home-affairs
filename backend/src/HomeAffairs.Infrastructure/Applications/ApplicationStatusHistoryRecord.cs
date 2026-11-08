using HomeAffairs.Domain.Applications;

namespace HomeAffairs.Infrastructure.Applications;

internal sealed class ApplicationStatusHistoryRecord
{
    public Guid ApplicationStatusHistoryId { get; init; }

    public Guid ApplicationId { get; init; }

    public string? FromStatus { get; init; }

    public string ToStatus { get; init; } =
        string.Empty;

    public Guid? ChangedByUserId { get; init; }

    public string? Reason { get; init; }

    public DateTime ChangedAtUtc { get; init; }

    public ApplicationStatusHistory ToDomain()
    {
        return new ApplicationStatusHistory
        {
            ApplicationStatusHistoryId =
                ApplicationStatusHistoryId,

            ApplicationId =
                ApplicationId,

            FromStatus =
                FromStatus,

            ToStatus =
                ToStatus,

            ChangedByUserId =
                ChangedByUserId,

            Reason =
                Reason,

            ChangedAtUtc =
                new DateTimeOffset(
                    DateTime.SpecifyKind(
                        ChangedAtUtc,
                        DateTimeKind.Utc))
        };
    }
}
