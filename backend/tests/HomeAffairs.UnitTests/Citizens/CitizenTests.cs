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

    [Fact]
    public void Citizen_should_be_minor_on_day_before_18th_birthday()
    {
        var citizen = new Citizen
        {
            DateOfBirth =
                new DateOnly(
                    2008,
                    11,
                    7)
        };

        var onDate =
            new DateOnly(
                2026,
                11,
                6);

        citizen.CalculateAge(onDate)
            .Should()
            .Be(17);

        citizen.IsMinor(onDate)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void Citizen_should_be_adult_on_18th_birthday()
    {
        var citizen = new Citizen
        {
            DateOfBirth =
                new DateOnly(
                    2008,
                    11,
                    6)
        };

        var onDate =
            new DateOnly(
                2026,
                11,
                6);

        citizen.CalculateAge(onDate)
            .Should()
            .Be(18);

        citizen.IsMinor(onDate)
            .Should()
            .BeFalse();
    }
}
