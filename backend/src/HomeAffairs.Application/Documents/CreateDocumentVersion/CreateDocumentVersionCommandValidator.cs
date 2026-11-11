using FluentValidation;
using HomeAffairs.Domain.Documents;

namespace HomeAffairs.Application.Documents.CreateDocumentVersion;

public sealed class CreateDocumentVersionCommandValidator
    : AbstractValidator<CreateDocumentVersionCommand>
{
    public CreateDocumentVersionCommandValidator()
    {
        RuleFor(command =>
                command.ApplicationId)
            .NotEmpty();

        RuleFor(command =>
                command.DocumentId)
            .NotEmpty();

        RuleFor(command =>
                command.FileName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(command =>
                command.ContentType)
            .NotEmpty()
            .Must(contentType =>
                SupportedDocumentFormats
                    .ContentTypes
                    .Contains(
                        contentType.Trim()))
            .WithMessage(
                "The file content type is not supported.");

        RuleFor(command =>
                command.Content)
            .NotNull()
            .Must(content =>
                content.Length > 0)
            .WithMessage(
                "The document is empty.");

        RuleFor(command =>
                command.Content)
            .Must(content =>
                content.LongLength <=
                SupportedDocumentFormats
                    .MaximumFileSizeBytes)
            .WithMessage(
                "The document exceeds the maximum " +
                "allowed size of 15 MB.");

        RuleFor(command =>
                command.ExpectedRowVersion)
            .NotNull()
            .Must(rowVersion =>
                rowVersion.Length == 8)
            .WithMessage(
                "The row version is invalid.");
    }
}
