using FluentAssertions;
using HomeAffairs.Domain.Applications;
using HomeAffairs.Domain.SmartId;

namespace HomeAffairs.UnitTests.SmartId;

public sealed class SmartIdSubmissionRulesTests
{
    [Fact]
    public void Complete_new_smart_id_details_should_be_valid()
    {
        var details =
            new SmartIdServiceDetails
            {
                ApplicationType =
                    ApplicationTypes.SmartIdNew,

                RequestedFirstName =
                    "Test",

                RequestedLastName =
                    "Citizen",

                RequestedDateOfBirth =
                    new DateOnly(
                        1990,
                        1,
                        1),

                RequestedSex =
                    "F"
            };

        SmartIdSubmissionRules
            .HasRequiredDetails(details)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void New_smart_id_without_name_should_be_invalid()
    {
        var details =
            new SmartIdServiceDetails
            {
                ApplicationType =
                    ApplicationTypes.SmartIdNew,

                RequestedDateOfBirth =
                    new DateOnly(
                        1990,
                        1,
                        1),

                RequestedSex =
                    "F"
            };

        SmartIdSubmissionRules
            .HasRequiredDetails(details)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void Lost_replacement_requires_existing_id_and_reason()
    {
        var valid =
            new SmartIdServiceDetails
            {
                ApplicationType =
                    ApplicationTypes
                        .SmartIdReplacementLost,

                ExistingSmartIdNumber =
                    "9001015009087",

                ReplacementReason =
                    "Card lost."
            };

        SmartIdSubmissionRules
            .HasRequiredDetails(valid)
            .Should()
            .BeTrue();

        var invalid =
            new SmartIdServiceDetails
            {
                ApplicationType =
                    ApplicationTypes
                        .SmartIdReplacementLost,

                ExistingSmartIdNumber =
                    "9001015009087"
            };

        SmartIdSubmissionRules
            .HasRequiredDetails(invalid)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void Stolen_replacement_requires_incident_and_police_information()
    {
        var valid =
            new SmartIdServiceDetails
            {
                ApplicationType =
                    ApplicationTypes
                        .SmartIdReplacementStolen,

                ExistingSmartIdNumber =
                    "9001015009087",

                ReplacementReason =
                    "Card stolen.",

                IncidentDate =
                    new DateOnly(
                        2026,
                        11,
                        1),

                PoliceCaseNumber =
                    "CAS-123",

                PoliceStation =
                    "Cape Town Central"
            };

        SmartIdSubmissionRules
            .HasRequiredDetails(valid)
            .Should()
            .BeTrue();

        var invalid =
            new SmartIdServiceDetails
            {
                ApplicationType =
                    ApplicationTypes
                        .SmartIdReplacementStolen,

                ExistingSmartIdNumber =
                    "9001015009087",

                ReplacementReason =
                    "Card stolen."
            };

        SmartIdSubmissionRules
            .HasRequiredDetails(invalid)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void Damaged_replacement_requires_damage_description()
    {
        var details =
            new SmartIdServiceDetails
            {
                ApplicationType =
                    ApplicationTypes
                        .SmartIdReplacementDamaged,

                ExistingSmartIdNumber =
                    "9001015009087",

                DamageDescription =
                    "Chip cannot be read."
            };

        SmartIdSubmissionRules
            .HasRequiredDetails(details)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void Correction_requires_old_and_new_values()
    {
        var complete =
            new SmartIdServiceDetails
            {
                ApplicationType =
                    ApplicationTypes
                        .SmartIdCorrection,

                ExistingSmartIdNumber =
                    "9001015009087",

                CorrectionField =
                    "LAST_NAME",

                CurrentValue =
                    "Old",

                RequestedValue =
                    "New",

                RequestReason =
                    "Surname change."
            };

        SmartIdSubmissionRules
            .HasRequiredDetails(complete)
            .Should()
            .BeTrue();

        complete = new SmartIdServiceDetails
        {
            ApplicationType =
                ApplicationTypes
                    .SmartIdCorrection,

            ExistingSmartIdNumber =
                "9001015009087",

            CorrectionField =
                "LAST_NAME",

            CurrentValue =
                "Old",

            RequestedValue =
                null,

            RequestReason =
                "Surname change."
        };

        SmartIdSubmissionRules
            .HasRequiredDetails(complete)
            .Should()
            .BeFalse();
    }
}
