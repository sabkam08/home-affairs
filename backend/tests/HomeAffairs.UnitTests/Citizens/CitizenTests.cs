using FluentAssertions;
using HomeAffairs.Domain.Citizens;

namespace HomeAffairs.UnitTests.Citizens;

public sealed class CitizenTests
{
    [Fact]
    public void Seventeen_year_old_should_be_minor()
    {
        var date =
            new DateOnly(
                2026,
                11,
                4);

        var citizen = new Citizen
        {
            DateOfBirth =
                new DateOnly(
                    2009,
                    11,
                    5)
        };

        citizen.IsMinor(date)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void Eighteen_year_old_should_not_be_minor()
    {
        var date =
            new DateOnly(
                2026,
                11,
                4);

        var citizen = new Citizen
        {
            DateOfBirth =
                new DateOnly(
                    2008,
                    11,
                    4)
        };

        citizen.IsMinor(date)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void Full_name_should_include_middle_names()
    {
        var citizen = new Citizen
        {
            FirstName = "Test",
            MiddleNames = "Middle Name",
            LastName = "Citizen"
        };

        citizen.FullName
            .Should()
            .Be(
                "Test Middle Name Citizen");
    }
}
