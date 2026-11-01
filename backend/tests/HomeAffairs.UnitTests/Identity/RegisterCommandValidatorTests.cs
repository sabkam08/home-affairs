using FluentAssertions;
using HomeAffairs.Application.Identity.Register;

namespace HomeAffairs.UnitTests.Identity;

public sealed class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public async Task Valid_registration_should_pass_validation()
    {
        var command = new RegisterCommand(
            "citizen@example.com",
            "HomeAffairs-Test-2026!",
            "Test",
            "Citizen");

        var result =
            await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Invalid_email_should_fail_validation()
    {
        var command = new RegisterCommand(
            "not-an-email",
            "HomeAffairs-Test-2026!",
            "Test",
            "Citizen");

        var result =
            await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();

        result.Errors
            .Should()
            .Contain(error =>
                error.PropertyName ==
                nameof(RegisterCommand.Email));
    }

    [Fact]
    public async Task Short_password_should_fail_validation()
    {
        var command = new RegisterCommand(
            "citizen@example.com",
            "Test1!",
            "Test",
            "Citizen");

        var result =
            await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();

        result.Errors
            .Should()
            .Contain(error =>
                error.PropertyName ==
                nameof(RegisterCommand.Password));
    }

    [Theory]
    [InlineData("homeaffairs-test-2026!")]
    [InlineData("HOMEAFFAIRS-TEST-2026!")]
    [InlineData("HomeAffairs-Test-Test!")]
    [InlineData("HomeAffairsTest2026")]
    public async Task Password_missing_required_character_type_should_fail(
        string password)
    {
        var command = new RegisterCommand(
            "citizen@example.com",
            password,
            "Test",
            "Citizen");

        var result =
            await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }
}
