using FluentValidation;

namespace HomeAffairs.Application.Identity.Administration;

public sealed class RemoveRoleCommandValidator
    : AbstractValidator<RemoveRoleCommand>
{
    public RemoveRoleCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty();

        RuleFor(command => command.RoleCode)
            .NotEmpty()
            .MaximumLength(100);
    }
}
