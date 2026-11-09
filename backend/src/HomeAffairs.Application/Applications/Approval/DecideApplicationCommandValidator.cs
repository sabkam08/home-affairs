using FluentValidation;
using HomeAffairs.Domain.Applications;

namespace HomeAffairs.Application.Applications.Approval;

public sealed class DecideApplicationCommandValidator
    : AbstractValidator<DecideApplicationCommand>
{
    private static readonly IReadOnlySet<string>
        Decisions =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase)
            {
                ApplicationStatuses.Approved,
                ApplicationStatuses.Rejected
            };

    public DecideApplicationCommandValidator()
    {
        RuleFor(command =>
                command.ApplicationId)
            .NotEmpty();

        RuleFor(command =>
                command.Decision)
            .NotEmpty()
            .Must(value =>
                Decisions.Contains(
                    value.Trim()))
            .WithMessage(
                "Decision must be APPROVED or REJECTED.");

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
                    command.Decision,
                    ApplicationStatuses.Rejected,
                    StringComparison.OrdinalIgnoreCase),
            () =>
            {
                RuleFor(command =>
                        command.Reason)
                    .NotEmpty()
                    .WithMessage(
                        "A rejection reason is required.");
            });
    }
}
