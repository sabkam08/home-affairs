using FluentValidation;

namespace HomeAffairs.Application.Identity.Refresh;

public sealed class RefreshTokenCommandValidator
    : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(command => command.RefreshToken)
            .NotEmpty()
            .MaximumLength(1000);
    }
}
