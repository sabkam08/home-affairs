using HomeAffairs.Application.Workflow.GetWorkflowDefinition;
using HomeAffairs.Contracts.Workflow;
using HomeAffairs.Application.Workflow.GetAvailableTransitions;
using HomeAffairs.Application.Workflow.GetWorkflowInstance;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeAffairs.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/workflows")]
public sealed class WorkflowController(
    ISender sender)
    : ControllerBase
{
    [HttpGet("{applicationType}")]
    [ProducesResponseType(
        typeof(WorkflowDefinitionResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<
        ActionResult<WorkflowDefinitionResponse>>
        GetWorkflow(
            string applicationType,
            CancellationToken cancellationToken)
    {
        var result =
            await sender.Send(
                new GetWorkflowDefinitionQuery(
                    applicationType),
                cancellationToken);

        return Ok(
            new WorkflowDefinitionResponse(
                result.WorkflowDefinitionId,
                result.ApplicationType,
                result.Name,
                result.Version,
                result.IsActive,
                result.States
                    .Select(state =>
                        new WorkflowStateResponse(
                            state.StateCode,
                            state.DisplayName,
                            state.Sequence,
                            state.IsInitial,
                            state.IsTerminal))
                    .ToArray(),
                result.Transitions
                    .Select(transition =>
                        new WorkflowTransitionResponse(
                            transition.FromStateCode,
                            transition.ToStateCode,
                            transition.ActionCode,
                            transition.DisplayName,
                            transition.RequiredPermission,
                            transition.RequiresReason))
                    .ToArray()));
    }

    [HttpGet(
    "applications/{applicationId:guid}")]
    [ProducesResponseType(
    typeof(WorkflowInstanceResponse),
    StatusCodes.Status200OK)]
    [ProducesResponseType(
    StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkflowInstanceResponse>>
    GetApplicationWorkflow(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var result =
            await sender.Send(
                new GetMyWorkflowInstanceQuery(
                    applicationId),
                cancellationToken);

        return Ok(
            new WorkflowInstanceResponse(
                result.WorkflowInstanceId,
                result.WorkflowDefinitionId,
                result.ApplicationId,
                result.ApplicationType,
                result.WorkflowName,
                result.WorkflowVersion,
                result.CurrentStateCode,
                result.StartedAtUtc,
                result.CompletedAtUtc,
                result.CreatedAtUtc,
                result.UpdatedAtUtc,
                Convert.ToBase64String(
                    result.RowVersion)));
    }

    [HttpGet(
    "applications/{applicationId:guid}/transitions")]
    [ProducesResponseType(
    typeof(
        IReadOnlyCollection<
            AvailableWorkflowTransitionResponse>),
    StatusCodes.Status200OK)]
    [ProducesResponseType(
    StatusCodes.Status404NotFound)]
    public async Task<
    ActionResult<
        IReadOnlyCollection<
            AvailableWorkflowTransitionResponse>>>
    GetAvailableTransitions(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var results =
            await sender.Send(
                new GetAvailableTransitionsQuery(
                    applicationId),
                cancellationToken);

        return Ok(
            results
                .Select(result =>
                    new AvailableWorkflowTransitionResponse(
                        result.FromStateCode,
                        result.ToStateCode,
                        result.ActionCode,
                        result.DisplayName,
                        result.RequiredPermission,
                        result.RequiresReason))
                .ToArray());
    }
}
