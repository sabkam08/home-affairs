using FluentValidation;
using HomeAffairs.Domain.Applications;

namespace HomeAffairs.Application.Documents.GetDocumentRequirements;

public sealed class GetDocumentRequirementsQueryValidator
    : AbstractValidator<GetDocumentRequirementsQuery>
{
    public GetDocumentRequirementsQueryValidator()
    {
        RuleFor(query =>
                query.ApplicationType)
            .NotEmpty()
            .Must(value =>
                ApplicationTypes.All.Contains(
                    value.Trim()))
            .WithMessage(
                "The application type is not supported.");
    }
}
