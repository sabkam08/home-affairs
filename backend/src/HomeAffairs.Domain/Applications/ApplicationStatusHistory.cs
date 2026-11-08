namespace HomeAffairs.Domain.Applications;

public sealed class ApplicationStatusHistory
{
    public Guid ApplicationStatusHistoryId { get; init; }

    public Guid ApplicationId { get; init; }

    public string? FromStatus { get; init; }

    public string ToStatus { get; init; } =
        string.Empty;

    public Guid? ChangedByUserId { get; init; }

    public string? Reason { get; init; }

    public DateTimeOffset ChangedAtUtc { get; init; }
}
