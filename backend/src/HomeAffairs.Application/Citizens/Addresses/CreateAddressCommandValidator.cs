using FluentValidation;
using HomeAffairs.Domain.Citizens;

namespace HomeAffairs.Application.Citizens.Addresses;

public sealed class CreateAddressCommandValidator
    : AbstractValidator<CreateAddressCommand>
{
    public CreateAddressCommandValidator()
    {
        RuleFor(x => x.AddressType)
            .NotEmpty()
            .Must(value =>
                AddressTypes.All.Contains(
                    value.Trim()));

        RuleFor(x => x.AddressLine1)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.AddressLine2)
            .MaximumLength(200);

        RuleFor(x => x.Suburb)
            .MaximumLength(150);

        RuleFor(x => x.CityOrTown)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Province)
            .MaximumLength(100);

        RuleFor(x => x.PostalCode)
            .MaximumLength(20);

        RuleFor(x => x.CountryCode)
            .NotEmpty()
            .Length(2);
    }
}
