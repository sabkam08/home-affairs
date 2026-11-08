using FluentValidation;

namespace HomeAffairs.Application.Applications.CancelApplication;

public sealed class CancelApplicationCommandValidator
    : AbstractValidator<CancelApplicationCommand>
{
    public CancelApplicationCommandValidator()
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

        RuleFor(command =>
                command.Reason)
            .MaximumLength(500);
    }
}
