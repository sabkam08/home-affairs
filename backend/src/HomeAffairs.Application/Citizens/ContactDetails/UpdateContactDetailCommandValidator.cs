using FluentValidation;
using HomeAffairs.Domain.Citizens;

namespace HomeAffairs.Application.Citizens.ContactDetails;

public sealed class UpdateContactDetailCommandValidator
    : AbstractValidator<UpdateContactDetailCommand>
{
    public UpdateContactDetailCommandValidator()
    {
        RuleFor(command =>
                command.ContactDetailId)
            .NotEmpty();

        RuleFor(command =>
                command.ContactType)
            .NotEmpty()
            .Must(value =>
                ContactTypes.All.Contains(
                    value.Trim()))
            .WithMessage(
                "ContactType must be EMAIL, MOBILE, " +
                "LANDLINE or ALTERNATIVE.");

        RuleFor(command =>
                command.ContactValue)
            .NotEmpty()
            .MaximumLength(320);

        RuleFor(command =>
                command.ExpectedRowVersion)
            .NotNull()
            .Must(value =>
                value.Length == 8)
            .WithMessage(
                "The row version is invalid.");

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
                    .EmailAddress();
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
