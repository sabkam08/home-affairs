using FluentValidation;

namespace HomeAffairs.Application.Applications.SubmitApplication;

public sealed class SubmitApplicationCommandValidator
    : AbstractValidator<SubmitApplicationCommand>
{
    public SubmitApplicationCommandValidator()
    {
        RuleFor(command =>
                command.ApplicationId)
            .NotEmpty();

        RuleFor(command =>
                command.ExpectedRowVersion)
            .NotNull()
            .Must(value =>
                value.Length == 8)
            .WithMessage(
                "The row version is invalid.");
    }
}
