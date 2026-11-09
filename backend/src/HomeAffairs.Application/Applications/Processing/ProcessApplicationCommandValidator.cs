using FluentValidation;
using HomeAffairs.Domain.Applications;

namespace HomeAffairs.Application.Applications.Processing;

public sealed class ProcessApplicationCommandValidator
    : AbstractValidator<ProcessApplicationCommand>
{
    private static readonly IReadOnlySet<string>
        PractitionerTargets =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase)
            {
                ApplicationStatuses.AwaitingInformation,
                ApplicationStatuses.AwaitingApproval,
                ApplicationStatuses.InProgress
            };

    public ProcessApplicationCommandValidator()
    {
        RuleFor(command =>
                command.ApplicationId)
            .NotEmpty();

        RuleFor(command =>
                command.TargetStatus)
            .NotEmpty()
            .Must(value =>
                PractitionerTargets.Contains(
                    value.Trim()))
            .WithMessage(
                "TargetStatus must be " +
                "AWAITING_INFORMATION, " +
                "AWAITING_APPROVAL or IN_PROGRESS.");

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

        When(
            command =>
                string.Equals(
                    command.TargetStatus,
                    ApplicationStatuses.AwaitingInformation,
                    StringComparison.OrdinalIgnoreCase),
            () =>
            {
                RuleFor(command =>
                        command.Reason)
                    .NotEmpty()
                    .WithMessage(
                        "A reason is required when " +
                        "requesting additional information.");
            });
    }
}
