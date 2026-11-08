using FluentValidation;
using HomeAffairs.Domain.Applications;

namespace HomeAffairs.Application.Applications.UpdateApplication;

public sealed class UpdateApplicationCommandValidator
    : AbstractValidator<UpdateApplicationCommand>
{
    public UpdateApplicationCommandValidator()
    {
        RuleFor(command =>
                command.ApplicationId)
            .NotEmpty();

        RuleFor(command =>
                command.ApplicationType)
            .NotEmpty()
            .Must(value =>
                ApplicationTypes.All.Contains(
                    value.Trim()))
            .WithMessage(
                "The application type is not supported.");

        RuleFor(command =>
                command.ExpectedRowVersion)
            .NotNull()
            .Must(value =>
                value.Length == 8)
            .WithMessage(
                "The row version is invalid.");
    }
}
