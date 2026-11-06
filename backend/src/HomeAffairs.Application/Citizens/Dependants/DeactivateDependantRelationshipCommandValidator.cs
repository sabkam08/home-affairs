using FluentValidation;

namespace HomeAffairs.Application.Citizens.Dependants;

public sealed class
    DeactivateDependantRelationshipCommandValidator
    : AbstractValidator<
        DeactivateDependantRelationshipCommand>
{
    public DeactivateDependantRelationshipCommandValidator()
    {
        RuleFor(command =>
                command.SubjectCitizenId)
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
