using FluentValidation;
using HomeAffairs.Domain.Citizens;

namespace HomeAffairs.Application.Citizens.ContactDetails;

public sealed class CreateContactDetailCommandValidator
    : AbstractValidator<CreateContactDetailCommand>
{
    public CreateContactDetailCommandValidator()
    {
        RuleFor(command => command.ContactType)
            .NotEmpty()
            .Must(value =>
                ContactTypes.All.Contains(
                    value.Trim()))
            .WithMessage(
                "ContactType must be EMAIL, MOBILE, " +
                "LANDLINE or ALTERNATIVE.");

        RuleFor(command => command.ContactValue)
            .NotEmpty()
            .MaximumLength(320);

        When(
            command =>
                string.Equals(
                    command.ContactType.Trim(),
                    ContactTypes.Email,
                    StringComparison.OrdinalIgnoreCase),
            () =>
            {
                RuleFor(command =>
                        command.ContactValue)
                    .EmailAddress()
                    .MaximumLength(320);
            });

        When(
            command =>
                string.Equals(
                    command.ContactType.Trim(),
                    ContactTypes.Mobile,
                    StringComparison.OrdinalIgnoreCase),
            () =>
            {
                RuleFor(command =>
                        command.ContactValue)
                    .Matches(
                        @"^\+?[0-9]{10,15}$")
                    .WithMessage(
                        "Mobile number must contain " +
                        "between 10 and 15 digits and " +
                        "may start with +.");
            });

        When(
            command =>
                string.Equals(
                    command.ContactType.Trim(),
                    ContactTypes.Landline,
                    StringComparison.OrdinalIgnoreCase),
            () =>
            {
                RuleFor(command =>
                        command.ContactValue)
                    .Matches(
                        @"^\+?[0-9]{9,15}$")
                    .WithMessage(
                        "Landline number must contain " +
                        "between 9 and 15 digits and " +
                        "may start with +.");
            });
    }
}
