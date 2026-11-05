using FluentValidation;
using HomeAffairs.Domain.Citizens;

namespace HomeAffairs.Application.Citizens.Addresses;

public sealed class UpdateAddressCommandValidator
    : AbstractValidator<UpdateAddressCommand>
{
    public UpdateAddressCommandValidator()
    {
        RuleFor(x => x.AddressId)
            .NotEmpty();

        RuleFor(x => x.AddressType)
            .NotEmpty()
            .Must(value =>
                AddressTypes.All.Contains(
                    value.Trim()));

        RuleFor(x => x.AddressLine1)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.CityOrTown)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.CountryCode)
            .NotEmpty()
            .Length(2);

        RuleFor(x => x.ExpectedRowVersion)
            .NotNull()
            .Must(value =>
                value.Length == 8);
    }
}
