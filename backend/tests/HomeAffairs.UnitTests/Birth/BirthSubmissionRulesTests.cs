using FluentAssertions;
using HomeAffairs.Domain.Applications;
using HomeAffairs.Domain.Birth;

namespace HomeAffairs.UnitTests.Birth;

public sealed class BirthSubmissionRulesTests
{
    [Fact]
    public void Complete_birth_registration_should_be_valid()
    {
        var details =
            CreateCompleteDetails(
                ApplicationTypes
                    .BirthRegistration);

        BirthSubmissionRules
            .HasRequiredDetails(details)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void Birth_registration_without_child_name_should_be_invalid()
    {
        var details =
            CreateCompleteDetails(
                ApplicationTypes
                    .BirthRegistration);

        details =
            Copy(
                details,
                childFirstName: null);

        BirthSubmissionRules
            .HasRequiredDetails(details)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void Late_birth_without_reason_should_be_invalid()
    {
        var details =
            CreateCompleteDetails(
                ApplicationTypes
                    .LateBirthRegistration);

        details =
            Copy(
                details,
                requestReason: null);

        BirthSubmissionRules
            .HasRequiredDetails(details)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void Reissue_requires_certificate_number_and_reason()
    {
        var valid =
            new BirthServiceDetails
            {
                ApplicationType =
                    ApplicationTypes
                        .BirthCertificateReissue,

                CurrentCertificateNumber =
                    "BC-001",

                RequestReason =
                    "Damaged certificate."
            };

        BirthSubmissionRules
            .HasRequiredDetails(valid)
            .Should()
            .BeTrue();

        var invalid =
            new BirthServiceDetails
            {
                ApplicationType =
                    ApplicationTypes
                        .BirthCertificateReissue,

                CurrentCertificateNumber =
                    null,

                RequestReason =
                    "Damaged certificate."
            };

        BirthSubmissionRules
            .HasRequiredDetails(invalid)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void Amendment_requires_certificate_number_and_reason()
    {
        var details =
            new BirthServiceDetails
            {
                ApplicationType =
                    ApplicationTypes
                        .BirthCertificateAmendment,

                CurrentCertificateNumber =
                    "BC-002",

                RequestReason =
                    "Correction required."
            };

        BirthSubmissionRules
            .HasRequiredDetails(details)
            .Should()
            .BeTrue();
    }

    private static BirthServiceDetails
        CreateCompleteDetails(
            string applicationType)
    {
        return new BirthServiceDetails
        {
            ApplicationType =
                applicationType,

            ChildFirstName =
                "Baby",

            ChildLastName =
                "Citizen",

            ChildDateOfBirth =
                new DateOnly(
                    2026,
                    1,
                    15),

            ChildSex =
                "F",

            PlaceOfBirth =
                "Cape Town",

            CountryOfBirth =
                "South Africa",

            MotherFirstName =
                "Test",

            MotherLastName =
                "Mother",

            RequestReason =
                "Late registration reason.",

            CurrentCertificateNumber =
                "BC-TEST"
        };
    }

    private static BirthServiceDetails Copy(
        BirthServiceDetails source,
        string? childFirstName = "Baby",
        string? requestReason =
            "Late registration reason.")
    {
        return new BirthServiceDetails
        {
            ApplicationType =
                source.ApplicationType,

            ChildFirstName =
                childFirstName,

            ChildLastName =
                source.ChildLastName,

            ChildDateOfBirth =
                source.ChildDateOfBirth,

            ChildSex =
                source.ChildSex,

            PlaceOfBirth =
                source.PlaceOfBirth,

            CountryOfBirth =
                source.CountryOfBirth,

            MotherFirstName =
                source.MotherFirstName,

            MotherLastName =
                source.MotherLastName,

            CurrentCertificateNumber =
                source.CurrentCertificateNumber,

            RequestReason =
                requestReason
        };
    }
}
