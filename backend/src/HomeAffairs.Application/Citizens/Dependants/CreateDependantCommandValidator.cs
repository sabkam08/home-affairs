using FluentValidation;
using HomeAffairs.Domain.Citizens;

namespace HomeAffairs.Application.Citizens.Dependants;

public sealed class CreateDependantCommandValidator
    : AbstractValidator<CreateDependantCommand>
{
    public CreateDependantCommandValidator()
    {
        RuleFor(command => command.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.MiddleNames)
            .MaximumLength(200);

        RuleFor(command => command.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.DateOfBirth)
            .NotEmpty()
            .Must(date =>
                date <= DateOnly.FromDateTime(
                    DateTime.UtcNow))
            .WithMessage(
                "Date of birth cannot be in the future.");

        RuleFor(command => command.DateOfBirth)
            .Must(date =>
            {
                var today =
                    DateOnly.FromDateTime(
                        DateTime.UtcNow);

                var age =
                    today.Year -
                    date.Year;

                if (today < date.AddYears(age))
                {
                    age--;
                }

                return age < 18;
            })
            .WithMessage(
                "The represented citizen must be under 18.");

        RuleFor(command => command.Sex)
            .NotEmpty()
            .Must(value =>
                value.Trim().ToUpperInvariant()
                    is "M" or "F" or "X")
            .WithMessage(
                "Sex must be M, F or X.");

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

        When(
            command =>
                !string.IsNullOrWhiteSpace(
                    command.SouthAfricanIdNumber),
            () =>
            {
                RuleFor(command =>
                        command.SouthAfricanIdNumber!)
                    .Matches(@"^\d{13}$")
                    .WithMessage(
                        "South African ID number must " +
                        "contain exactly 13 digits.");
            });
    }
}
