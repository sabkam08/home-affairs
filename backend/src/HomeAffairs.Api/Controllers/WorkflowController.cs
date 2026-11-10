using HomeAffairs.Application.Workflow.GetWorkflowDefinition;
using HomeAffairs.Contracts.Workflow;
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
}
