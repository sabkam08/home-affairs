using FluentValidation;

namespace HomeAffairs.Application.Citizens.CreateProfile;

public sealed class CreateCitizenProfileCommandValidator
    : AbstractValidator<CreateCitizenProfileCommand>
{
    public CreateCitizenProfileCommandValidator()
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
                date <=
                DateOnly.FromDateTime(
                    DateTime.UtcNow))
            .WithMessage(
                "Date of birth cannot be in the future.");

        RuleFor(command => command.Sex)
            .NotEmpty()
            .Must(value =>
                value is "M" or "F" or "X")
            .WithMessage(
                "Sex must be M, F or X.");

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
