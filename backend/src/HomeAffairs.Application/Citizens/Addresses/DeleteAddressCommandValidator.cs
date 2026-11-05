using FluentValidation;

namespace HomeAffairs.Application.Citizens.Addresses;

public sealed class DeleteAddressCommandValidator
    : AbstractValidator<DeleteAddressCommand>
{
    public DeleteAddressCommandValidator()
    {
        RuleFor(x => x.AddressId)
            .NotEmpty();

        RuleFor(x => x.ExpectedRowVersion)
            .NotNull()
            .Must(value =>
                value.Length == 8);
    }
}
