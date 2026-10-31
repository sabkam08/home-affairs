using FluentValidation;

namespace HomeAffairs.Application.Identity.Administration;

public sealed class AssignRoleCommandValidator
    : AbstractValidator<AssignRoleCommand>
{
    public AssignRoleCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty();

        RuleFor(command => command.RoleCode)
            .NotEmpty()
            .MaximumLength(100);
    }
}
