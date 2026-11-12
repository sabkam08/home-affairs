using FluentValidation;

namespace HomeAffairs.Application.Documents.SearchDocumentText;

public sealed class SearchDocumentTextQueryValidator
    : AbstractValidator<SearchDocumentTextQuery>
{
    public SearchDocumentTextQueryValidator()
    {
        RuleFor(query =>
                query.ApplicationId)
            .NotEmpty();

        RuleFor(query =>
                query.SearchText)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(500);
    }
}
