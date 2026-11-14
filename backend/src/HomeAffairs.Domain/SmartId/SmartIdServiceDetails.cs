namespace HomeAffairs.Domain.SmartId;

public sealed class SmartIdServiceDetails
{
    public Guid SmartIdServiceDetailsId { get; init; }

    public Guid ApplicationId { get; init; }

    public string ApplicationType { get; init; } =
        string.Empty;

    public string? ExistingSmartIdNumber { get; init; }

    public string? RequestedFirstName { get; init; }

    public string? RequestedMiddleNames { get; init; }

    public string? RequestedLastName { get; init; }

    public DateOnly? RequestedDateOfBirth { get; init; }

    public string? RequestedSex { get; init; }

    public string? ReplacementReason { get; init; }

    public DateOnly? IncidentDate { get; init; }

    public string? PoliceCaseNumber { get; init; }

    public string? PoliceStation { get; init; }

    public string? DamageDescription { get; init; }

    public string? CorrectionField { get; init; }

    public string? CurrentValue { get; init; }

    public string? RequestedValue { get; init; }

    public string? RequestReason { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? UpdatedAtUtc { get; init; }

    public byte[] RowVersion { get; init; } = [];
}
