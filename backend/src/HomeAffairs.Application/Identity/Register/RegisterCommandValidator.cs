using FluentValidation;

namespace HomeAffairs.Application.Identity.Register;

public sealed class RegisterCommandValidator
    : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
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
            .WithMessage(
                "Password must contain at least one uppercase letter.")
            .Matches("[a-z]")
            .WithMessage(
                "Password must contain at least one lowercase letter.")
            .Matches("[0-9]")
            .WithMessage(
                "Password must contain at least one number.")
            .Matches("[^a-zA-Z0-9]")
            .WithMessage(
                "Password must contain at least one special character.");

        RuleFor(command => command.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.LastName)
            .NotEmpty()
            .MaximumLength(100);
    }
}
