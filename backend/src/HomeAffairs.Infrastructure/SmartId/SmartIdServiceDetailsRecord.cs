using HomeAffairs.Domain.SmartId;

namespace HomeAffairs.Infrastructure.SmartId;

internal sealed class SmartIdServiceDetailsRecord
{
    public Guid SmartIdServiceDetailsId { get; init; }

    public Guid ApplicationId { get; init; }

    public string ApplicationType { get; init; } =
        string.Empty;

    public string? ExistingSmartIdNumber { get; init; }

    public string? RequestedFirstName { get; init; }

    public string? RequestedMiddleNames { get; init; }

    public string? RequestedLastName { get; init; }

    public DateTime? RequestedDateOfBirth { get; init; }

    public string? RequestedSex { get; init; }

    public string? ReplacementReason { get; init; }

    public DateTime? IncidentDate { get; init; }

    public string? PoliceCaseNumber { get; init; }

    public string? PoliceStation { get; init; }

    public string? DamageDescription { get; init; }

    public string? CorrectionField { get; init; }

    public string? CurrentValue { get; init; }

    public string? RequestedValue { get; init; }

    public string? RequestReason { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public byte[] RowVersion { get; init; } = [];

    public SmartIdServiceDetails ToDomain()
    {
        return new SmartIdServiceDetails
        {
            SmartIdServiceDetailsId =
                SmartIdServiceDetailsId,

            ApplicationId =
                ApplicationId,

            ApplicationType =
                ApplicationType,

            ExistingSmartIdNumber =
                ExistingSmartIdNumber,

            RequestedFirstName =
                RequestedFirstName,

            RequestedMiddleNames =
                RequestedMiddleNames,

            RequestedLastName =
                RequestedLastName,

            RequestedDateOfBirth =
                RequestedDateOfBirth.HasValue
                    ? DateOnly.FromDateTime(
                        RequestedDateOfBirth.Value)
                    : null,

            RequestedSex =
                RequestedSex,

            ReplacementReason =
                ReplacementReason,

            IncidentDate =
                IncidentDate.HasValue
                    ? DateOnly.FromDateTime(
                        IncidentDate.Value)
                    : null,

            PoliceCaseNumber =
                PoliceCaseNumber,

            PoliceStation =
                PoliceStation,

            DamageDescription =
                DamageDescription,

            CorrectionField =
                CorrectionField,

            CurrentValue =
                CurrentValue,

            RequestedValue =
                RequestedValue,

            RequestReason =
                RequestReason,

            CreatedAtUtc =
                ToUtc(CreatedAtUtc),

            UpdatedAtUtc =
                UpdatedAtUtc.HasValue
                    ? ToUtc(UpdatedAtUtc.Value)
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
