using HomeAffairs.Domain.Applications;

namespace HomeAffairs.Domain.SmartId;

public static class SmartIdSubmissionRules
{
    public static bool HasRequiredDetails(
        SmartIdServiceDetails details)
    {
        return details.ApplicationType switch
        {
            ApplicationTypes.SmartIdNew =>
                HasValue(
                    details.RequestedFirstName)
                &&
                HasValue(
                    details.RequestedLastName)
                &&
                details.RequestedDateOfBirth
                    .HasValue
                &&
                HasValue(
                    details.RequestedSex),

            ApplicationTypes
                .SmartIdReplacementLost =>
                HasValue(
                    details.ExistingSmartIdNumber)
                &&
                HasValue(
                    details.ReplacementReason),

            ApplicationTypes
                .SmartIdReplacementStolen =>
                HasValue(
                    details.ExistingSmartIdNumber)
                &&
                HasValue(
                    details.ReplacementReason)
                &&
                details.IncidentDate.HasValue
                &&
                HasValue(
                    details.PoliceCaseNumber)
                &&
                HasValue(
                    details.PoliceStation),

            ApplicationTypes
                .SmartIdReplacementDamaged =>
                HasValue(
                    details.ExistingSmartIdNumber)
                &&
                HasValue(
                    details.DamageDescription),

            ApplicationTypes
                .SmartIdCorrection =>
                HasValue(
                    details.ExistingSmartIdNumber)
                &&
                HasValue(
                    details.CorrectionField)
                &&
                HasValue(
                    details.CurrentValue)
                &&
                HasValue(
                    details.RequestedValue)
                &&
                HasValue(
                    details.RequestReason),

            _ =>
                false
        };
    }

    private static bool HasValue(
        string? value)
    {
        return !string.IsNullOrWhiteSpace(
            value);
    }
}
