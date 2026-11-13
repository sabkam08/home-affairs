using FluentAssertions;
using HomeAffairs.Domain.Birth;

namespace HomeAffairs.UnitTests.Birth;

public sealed class BirthServiceTests
{
    [Fact]
    public void New_birth_details_should_preserve_application_id()
    {
        var applicationId =
            Guid.NewGuid();

        var details =
            new BirthServiceDetails
            {
                ApplicationId =
                    applicationId,

                ChildFirstName =
                    "Test",

                ChildLastName =
                    "Child"
            };

        details.ApplicationId
            .Should()
            .Be(applicationId);
    }

    [Fact]
    public void Birth_details_should_support_optional_father_information()
    {
        var details =
            new BirthServiceDetails
            {
                ChildFirstName =
                    "Test",

                MotherFirstName =
                    "Parent",

                FatherFirstName =
                    null,

                FatherLastName =
                    null
            };

        details.FatherFirstName
            .Should()
            .BeNull();

        details.FatherLastName
            .Should()
            .BeNull();
    }
}
