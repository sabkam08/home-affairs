using FluentValidation;
using HomeAffairs.Domain.Documents;

namespace HomeAffairs.Application.Documents.UploadDocument;

public sealed class UploadDocumentCommandValidator
    : AbstractValidator<UploadDocumentCommand>
{
    public UploadDocumentCommandValidator()
    {
        RuleFor(command =>
                command.ApplicationId)
            .NotEmpty();

        RuleFor(command =>
                command.DocumentType)
            .NotEmpty()
            .Must(value =>
                DocumentTypes.All.Contains(
                    value.Trim()))
            .WithMessage(
                "The document type is not supported.");

        RuleFor(command =>
                command.FileName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(command =>
                command.ContentType)
            .NotEmpty()
            .Must(value =>
                SupportedDocumentFormats
                    .ContentTypes
                    .Contains(
                        value.Trim()))
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
    }
}
