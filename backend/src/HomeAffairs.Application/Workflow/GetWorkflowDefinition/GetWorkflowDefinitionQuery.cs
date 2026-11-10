using MediatR;

namespace HomeAffairs.Application.Workflow.GetWorkflowDefinition;

public sealed record GetWorkflowDefinitionQuery(
	string ApplicationType)
	: IRequest<WorkflowDefinitionResult>;
