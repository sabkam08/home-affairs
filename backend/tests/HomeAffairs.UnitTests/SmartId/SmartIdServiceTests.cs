using FluentAssertions;
using HomeAffairs.Domain.SmartId;

namespace HomeAffairs.UnitTests.SmartId;

public sealed class SmartIdServiceTests
{
    [Fact]
    public void Smart_id_details_should_preserve_application_id()
    {
        var applicationId =
            Guid.NewGuid();

        var details =
            new SmartIdServiceDetails
            {
                ApplicationId =
                    applicationId,

                ExistingSmartIdNumber =
                    "9001015009087"
            };

        details.ApplicationId
            .Should()
            .Be(applicationId);
    }

    [Fact]
    public void New_smart_id_should_support_requested_identity_details()
    {
        var details =
            new SmartIdServiceDetails
            {
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

        details.RequestedFirstName
            .Should()
            .Be("Test");

        details.RequestedLastName
            .Should()
            .Be("Citizen");
    }

    [Fact]
    public void Replacement_details_should_support_incident_information()
    {
        var incidentDate =
            new DateOnly(
                2026,
                11,
                1);

        var details =
            new SmartIdServiceDetails
            {
                ExistingSmartIdNumber =
                    "9001015009087",

                IncidentDate =
                    incidentDate,

                PoliceCaseNumber =
                    "CAS-123-11-2026",

                PoliceStation =
                    "Cape Town Central"
            };

        details.IncidentDate
            .Should()
            .Be(incidentDate);

        details.PoliceCaseNumber
            .Should()
            .Be("CAS-123-11-2026");
    }
}
