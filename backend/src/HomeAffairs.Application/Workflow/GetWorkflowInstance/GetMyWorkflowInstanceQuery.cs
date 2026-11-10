using MediatR;

namespace HomeAffairs.Application.Workflow.GetWorkflowInstance;

public sealed record GetMyWorkflowInstanceQuery(
    Guid ApplicationId)
    : IRequest<WorkflowInstanceResult>;
