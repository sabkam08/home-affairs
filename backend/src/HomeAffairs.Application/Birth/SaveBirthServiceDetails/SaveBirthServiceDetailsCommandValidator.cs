using FluentValidation;

namespace HomeAffairs.Application.Birth.SaveBirthServiceDetails;

public sealed class SaveBirthServiceDetailsCommandValidator
    : AbstractValidator<SaveBirthServiceDetailsCommand>
{
    public SaveBirthServiceDetailsCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty();

        RuleFor(x => x.ChildFirstName)
            .MaximumLength(100);

        RuleFor(x => x.ChildMiddleNames)
            .MaximumLength(200);

        RuleFor(x => x.ChildLastName)
            .MaximumLength(100);

        RuleFor(x => x.PlaceOfBirth)
            .MaximumLength(200);

        RuleFor(x => x.ProvinceOfBirth)
            .MaximumLength(100);

        RuleFor(x => x.CountryOfBirth)
            .MaximumLength(100);

        RuleFor(x => x.MotherFirstName)
            .MaximumLength(100);

        RuleFor(x => x.MotherLastName)
            .MaximumLength(100);

        RuleFor(x => x.MotherIdNumber)
            .MaximumLength(30);

        RuleFor(x => x.FatherFirstName)
            .MaximumLength(100);

        RuleFor(x => x.FatherLastName)
            .MaximumLength(100);

        RuleFor(x => x.FatherIdNumber)
            .MaximumLength(30);

        RuleFor(x => x.CurrentCertificateNumber)
            .MaximumLength(100);

        RuleFor(x => x.RequestReason)
            .MaximumLength(1000);

        When(
            x => !string.IsNullOrWhiteSpace(
                x.ChildSex),
            () =>
            {
                RuleFor(x => x.ChildSex!)
                    .Must(value =>
                        value.Trim().ToUpperInvariant()
                            is "M" or "F" or "X")
                    .WithMessage(
                        "ChildSex must be M, F or X.");
            });

        When(
            x => x.ExpectedRowVersion is not null,
            () =>
            {
                RuleFor(x =>
                        x.ExpectedRowVersion!)
                    .Must(value =>
                        value.Length == 8)
                    .WithMessage(
                        "The row version is invalid.");
            });
    }
}
