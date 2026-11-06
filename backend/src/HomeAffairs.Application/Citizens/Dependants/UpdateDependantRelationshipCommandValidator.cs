using FluentValidation;
using HomeAffairs.Domain.Citizens;

namespace HomeAffairs.Application.Citizens.Dependants;

public sealed class UpdateDependantRelationshipCommandValidator
    : AbstractValidator<
        UpdateDependantRelationshipCommand>
{
    public UpdateDependantRelationshipCommandValidator()
    {
        RuleFor(command =>
                command.SubjectCitizenId)
            .NotEmpty();

        RuleFor(command =>
                command.RelationshipType)
            .NotEmpty()
            .Must(value =>
                CitizenRelationshipTypes.All.Contains(
                    value.Trim()))
            .WithMessage(
                "RelationshipType must be " +
                "BIOLOGICAL_PARENT, ADOPTIVE_PARENT " +
                "or LEGAL_GUARDIAN.");

        RuleFor(command =>
                command.ExpectedRowVersion)
            .NotNull()
            .Must(value =>
                value.Length == 8)
            .WithMessage(
                "The row version is invalid.");
    }
}
