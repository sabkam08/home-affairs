using FluentValidation;

namespace HomeAffairs.Application.Identity.Administration;

public sealed class CreateStaffUserCommandValidator
    : AbstractValidator<CreateStaffUserCommand>
{
    public CreateStaffUserCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(command => command.Password)
            .NotEmpty()
            .MinimumLength(12)
            .MaximumLength(128)
            .Matches("[A-Z]")
            .Matches("[a-z]")
            .Matches("[0-9]")
            .Matches("[^a-zA-Z0-9]");

        RuleFor(command => command.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.RoleCode)
            .NotEmpty()
            .Must(role =>
                Roles.StaffRoles.Contains(role))
            .WithMessage(
                "The supplied staff role is not valid.");
    }
}
