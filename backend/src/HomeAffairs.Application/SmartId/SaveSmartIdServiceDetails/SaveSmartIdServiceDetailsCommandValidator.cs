using FluentValidation;

namespace HomeAffairs.Application.SmartId.SaveSmartIdServiceDetails;

public sealed class SaveSmartIdServiceDetailsCommandValidator
    : AbstractValidator<
        SaveSmartIdServiceDetailsCommand>
{
    public SaveSmartIdServiceDetailsCommandValidator()
    {
        RuleFor(command =>
                command.ApplicationId)
            .NotEmpty();

        RuleFor(command =>
                command.ExistingSmartIdNumber)
            .MaximumLength(30);

        RuleFor(command =>
                command.RequestedFirstName)
            .MaximumLength(100);

        RuleFor(command =>
                command.RequestedMiddleNames)
            .MaximumLength(200);

        RuleFor(command =>
                command.RequestedLastName)
            .MaximumLength(100);

        RuleFor(command =>
                command.ReplacementReason)
            .MaximumLength(1000);

        RuleFor(command =>
                command.PoliceCaseNumber)
            .MaximumLength(100);

        RuleFor(command =>
                command.PoliceStation)
            .MaximumLength(200);

        RuleFor(command =>
                command.DamageDescription)
            .MaximumLength(1000);

        RuleFor(command =>
                command.CorrectionField)
            .MaximumLength(100);

        RuleFor(command =>
                command.CurrentValue)
            .MaximumLength(500);

        RuleFor(command =>
                command.RequestedValue)
            .MaximumLength(500);

        RuleFor(command =>
                command.RequestReason)
            .MaximumLength(1000);

        When(
            command =>
                !string.IsNullOrWhiteSpace(
                    command.RequestedSex),
            () =>
            {
                RuleFor(command =>
                        command.RequestedSex!)
                    .Must(value =>
                        value
                            .Trim()
                            .ToUpperInvariant()
                        is "M" or "F" or "X")
                    .WithMessage(
                        "RequestedSex must be M, F or X.");
            });

        When(
            command =>
                command.ExpectedRowVersion
                is not null,
            () =>
            {
                RuleFor(command =>
                        command.ExpectedRowVersion!)
                    .Must(value =>
                        value.Length == 8)
                    .WithMessage(
                        "The row version is invalid.");
            });
    }
}
