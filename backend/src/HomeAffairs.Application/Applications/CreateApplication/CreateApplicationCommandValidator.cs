using FluentValidation;
using HomeAffairs.Domain.Applications;

namespace HomeAffairs.Application.Applications.CreateApplication;

public sealed class CreateApplicationCommandValidator
    : AbstractValidator<CreateApplicationCommand>
{
    public CreateApplicationCommandValidator()
    {
        RuleFor(command =>
                command.ApplicationType)
            .NotEmpty()
            .Must(value =>
                ApplicationTypes.All.Contains(
                    value.Trim()))
            .WithMessage(
                "The application type is not supported.");
    }
}
