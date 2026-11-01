using FluentAssertions;
using HomeAffairs.Application.Identity.Administration;

namespace HomeAffairs.UnitTests.Identity;

public sealed class CreateStaffUserCommandValidatorTests
{
    private readonly CreateStaffUserCommandValidator
        _validator = new();

    [Theory]
    [InlineData("PRACTITIONER")]
    [InlineData("SUPERVISOR")]
    [InlineData("ADMINISTRATOR")]
    [InlineData("SECURITY_ADMINISTRATOR")]
    public async Task Supported_staff_roles_should_pass(
        string role)
    {
        var command = CreateCommand(role);

        var result =
            await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("CITIZEN")]
    [InlineData("SUPERUSER")]
    [InlineData("ROOT")]
    [InlineData("")]
    public async Task Unsupported_staff_roles_should_fail(
        string role)
    {
        var command = CreateCommand(role);

        var result =
            await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    private static CreateStaffUserCommand
        CreateCommand(string role)
    {
        return new CreateStaffUserCommand(
            "staff@example.com",
            "HomeAffairs-Test-2026!",
            "Test",
            "Staff",
            role);
    }
}
