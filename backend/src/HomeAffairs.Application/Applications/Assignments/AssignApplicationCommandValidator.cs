using FluentValidation;

namespace HomeAffairs.Application.Applications.Assignments;

public sealed class AssignApplicationCommandValidator
    : AbstractValidator<AssignApplicationCommand>
{
    public AssignApplicationCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty();

        RuleFor(x => x.AssignedToUserId)
            .NotEmpty();

        RuleFor(x => x.ExpectedApplicationRowVersion)
            .NotNull()
            .Must(x => x.Length == 8);

        RuleFor(x => x.Reason)
            .MaximumLength(500);
    }
}
