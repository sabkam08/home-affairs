using FluentValidation;

namespace HomeAffairs.Application.Documents.DeactivateDocument;

public sealed class DeactivateDocumentCommandValidator
    : AbstractValidator<DeactivateDocumentCommand>
{
    public DeactivateDocumentCommandValidator()
    {
        RuleFor(command =>
                command.ApplicationId)
            .NotEmpty();

        RuleFor(command =>
                command.DocumentId)
            .NotEmpty();

        RuleFor(command =>
                command.ExpectedRowVersion)
            .NotNull()
            .Must(rowVersion =>
                rowVersion.Length == 8)
            .WithMessage(
                "The row version is invalid.");
    }
}
