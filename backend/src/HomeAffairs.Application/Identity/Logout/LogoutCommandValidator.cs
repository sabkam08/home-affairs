using FluentValidation;

namespace HomeAffairs.Application.Identity.Logout;

public sealed class LogoutCommandValidator
    : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(command => command.RefreshToken)
            .NotEmpty()
            .MaximumLength(1000);
    }
}
