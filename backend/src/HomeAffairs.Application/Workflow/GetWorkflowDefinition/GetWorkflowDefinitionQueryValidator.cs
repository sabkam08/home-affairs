using FluentValidation;
using HomeAffairs.Domain.Applications;

namespace HomeAffairs.Application.Workflow.GetWorkflowDefinition;

public sealed class GetWorkflowDefinitionQueryValidator
    : AbstractValidator<GetWorkflowDefinitionQuery>
{
    public GetWorkflowDefinitionQueryValidator()
    {
        RuleFor(query =>
                query.ApplicationType)
            .NotEmpty()
            .Must(type =>
                ApplicationTypes.All.Contains(
                    type.Trim()))
            .WithMessage(
                "The application type is not supported.");
    }
}
