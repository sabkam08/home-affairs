using FluentValidation;

namespace HomeAffairs.Application.Citizens.ContactDetails;

public sealed class DeleteContactDetailCommandValidator
    : AbstractValidator<DeleteContactDetailCommand>
{
    public DeleteContactDetailCommandValidator()
    {
        RuleFor(command =>
                command.ContactDetailId)
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
