using HomeAffairs.Api.Authorization;
using HomeAffairs.Application.Workflow.WorkItems;
using HomeAffairs.Contracts.Workflow;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeAffairs.Api.Controllers;

[ApiController]
[Route("api/v1/work-items")]
public sealed class WorkItemsController(
    ISender sender)
    : ControllerBase
{
    [Authorize(
        Policy =
            AuthorizationPolicies.ApplicationProcess)]
    [HttpGet("mine")]
    public async Task<
        ActionResult<
            IReadOnlyCollection<
                WorkflowWorkItemResponse>>>
        GetMine(
            CancellationToken cancellationToken)
    {
        var results =
            await sender.Send(
                new GetMyWorkItemsQuery(),
                cancellationToken);

        return Ok(
            results
                .Select(Map)
                .ToArray());
    }

    [Authorize(
        Policy =
            AuthorizationPolicies.ApplicationApprove)]
    [HttpGet("supervisor")]
    public async Task<
        ActionResult<
            IReadOnlyCollection<
                WorkflowWorkItemResponse>>>
        GetSupervisorQueue(
            CancellationToken cancellationToken)
    {
        var results =
            await sender.Send(
                new GetSupervisorWorkItemsQuery(),
                cancellationToken);

        return Ok(
            results
                .Select(Map)
                .ToArray());
    }

    [Authorize(
        Policy =
            AuthorizationPolicies.ApplicationReadAll)]
    [HttpGet(
        "applications/{applicationId:guid}/history")]
    public async Task<
        ActionResult<
            IReadOnlyCollection<
                WorkflowWorkItemResponse>>>
        GetApplicationWorkItemHistory(
            Guid applicationId,
            CancellationToken cancellationToken)
    {
        var results =
            await sender.Send(
                new GetApplicationWorkItemHistoryQuery(
                    applicationId),
                cancellationToken);

        return Ok(
            results
                .Select(Map)
                .ToArray());
    }

    private static WorkflowWorkItemResponse Map(
        WorkflowWorkItemResult result)
    {
        return new WorkflowWorkItemResponse(
            result.WorkItemId,
            result.WorkflowInstanceId,
            result.ApplicationId,
            result.WorkItemType,
            result.Status,
            result.AssignedToUserId,
            result.AssignedRoleCode,
            result.CreatedAtUtc,
            result.ClaimedAtUtc,
            result.CompletedAtUtc,
            result.CancelledAtUtc,
            result.CompletedByUserId,
            result.Reason,
            Convert.ToBase64String(
                result.RowVersion));
    }
}
