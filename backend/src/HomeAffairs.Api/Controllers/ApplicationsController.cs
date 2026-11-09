using HomeAffairs.Api.Authorization;
using HomeAffairs.Application.Applications;
using HomeAffairs.Application.Applications.Assignments;
using HomeAffairs.Application.Applications.CreateApplication;
using HomeAffairs.Application.Applications.GetApplication;
using HomeAffairs.Application.Applications.GetMyApplications;
using HomeAffairs.Application.Applications.UpdateApplication;
using HomeAffairs.Application.Applications.SubmitApplication;
using HomeAffairs.Application.Applications.CancelApplication;
using HomeAffairs.Application.Applications.GetApplicationHistory;
using HomeAffairs.Application.Applications.Processing;
using HomeAffairs.Application.Applications.Approval;
using HomeAffairs.Contracts.Applications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeAffairs.Api.Controllers;

[ApiController]
[Route("api/v1/applications")]
public sealed class ApplicationsController(
    ISender sender)
    : ControllerBase
{
    [Authorize(
        Policy =
            AuthorizationPolicies.ApplicationCreate)]
    [HttpPost]
    [ProducesResponseType(
        typeof(ApplicationResponse),
        StatusCodes.Status201Created)]
    public async Task<ActionResult<ApplicationResponse>>
        CreateApplication(
            CreateApplicationRequest request,
            CancellationToken cancellationToken)
    {
        var result =
            await sender.Send(
                new CreateApplicationCommand(
                    request.ApplicationType,
                    request.SubjectCitizenId),
                cancellationToken);

        var response =
            Map(result);

        return Created(
            $"/api/v1/applications/" +
            $"{result.ApplicationId}",
            response);
    }

    [Authorize]
    [HttpGet("{applicationId:guid}")]
    [ProducesResponseType(
        typeof(ApplicationResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApplicationResponse>>
        GetApplication(
            Guid applicationId,
            CancellationToken cancellationToken)
    {
        var result =
            await sender.Send(
                new GetApplicationQuery(
                    applicationId),
                cancellationToken);

        return Ok(Map(result));
    }

    [Authorize]
    [HttpGet("mine")]
    [ProducesResponseType(
        typeof(IReadOnlyCollection<
            ApplicationResponse>),
        StatusCodes.Status200OK)]
    public async Task<
        ActionResult<
            IReadOnlyCollection<
                ApplicationResponse>>>
        GetMyApplications(
            CancellationToken cancellationToken)
    {
        var results =
            await sender.Send(
                new GetMyApplicationsQuery(),
                cancellationToken);

        return Ok(
            results
                .Select(Map)
                .ToArray());
    }

    [Authorize(
    Policy =
        AuthorizationPolicies.ApplicationCreate)]
    [HttpPut("{applicationId:guid}")]
    public async Task<ActionResult<ApplicationResponse>>
    UpdateApplication(
        Guid applicationId,
        UpdateApplicationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryDecodeRowVersion(
                request.RowVersion,
                out var rowVersion))
        {
            ModelState.AddModelError(
                nameof(request.RowVersion),
                "RowVersion must be valid Base64.");

            return ValidationProblem(ModelState);
        }

        var result =
            await sender.Send(
                new UpdateApplicationCommand(
                    applicationId,
                    request.ApplicationType,
                    request.SubjectCitizenId,
                    rowVersion!),
                cancellationToken);

        return Ok(Map(result));
    }

    [Authorize(
    Policy =
        AuthorizationPolicies.ApplicationCreate)]
    [HttpPost("{applicationId:guid}/submit")]
    public async Task<ActionResult<ApplicationResponse>>
    SubmitApplication(
        Guid applicationId,
        ApplicationActionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryDecodeRowVersion(
                request.RowVersion,
                out var rowVersion))
        {
            ModelState.AddModelError(
                nameof(request.RowVersion),
                "RowVersion must be valid Base64.");

            return ValidationProblem(ModelState);
        }

        var result =
            await sender.Send(
                new SubmitApplicationCommand(
                    applicationId,
                    rowVersion!),
                cancellationToken);

        return Ok(Map(result));
    }

    [Authorize(
    Policy =
        AuthorizationPolicies.ApplicationCreate)]
    [HttpPost("{applicationId:guid}/cancel")]
    public async Task<ActionResult<ApplicationResponse>>
    CancelApplication(
        Guid applicationId,
        CancelApplicationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryDecodeRowVersion(
                request.RowVersion,
                out var rowVersion))
        {
            ModelState.AddModelError(
                nameof(request.RowVersion),
                "RowVersion must be valid Base64.");

            return ValidationProblem(ModelState);
        }

        var result =
            await sender.Send(
                new CancelApplicationCommand(
                    applicationId,
                    rowVersion!,
                    request.Reason),
                cancellationToken);

        return Ok(Map(result));
    }

    [Authorize]
    [HttpGet("{applicationId:guid}/history")]
    public async Task<
    ActionResult<
        IReadOnlyCollection<
            ApplicationStatusHistoryResponse>>>
    GetApplicationHistory(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var results =
            await sender.Send(
                new GetApplicationHistoryQuery(
                    applicationId),
                cancellationToken);

        return Ok(
            results
                .Select(item =>
                    new ApplicationStatusHistoryResponse(
                        item.ApplicationStatusHistoryId,
                        item.ApplicationId,
                        item.FromStatus,
                        item.ToStatus,
                        item.ChangedByUserId,
                        item.Reason,
                        item.ChangedAtUtc))
                .ToArray());
    }

    [Authorize(
    Policy =
        AuthorizationPolicies.ApplicationReassign)]
    [HttpPut("{applicationId:guid}/assignment")]
    public async Task<
    ActionResult<ApplicationAssignmentResponse>>
    AssignApplication(
        Guid applicationId,
        AssignApplicationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryDecodeRowVersion(
                request.RowVersion,
                out var rowVersion))
        {
            ModelState.AddModelError(
                nameof(request.RowVersion),
                "RowVersion must be valid Base64.");

            return ValidationProblem(ModelState);
        }

        var result =
            await sender.Send(
                new AssignApplicationCommand(
                    applicationId,
                    request.AssignedToUserId,
                    rowVersion!,
                    request.Reason),
                cancellationToken);

        return Ok(
            MapAssignment(result));
    }

    [Authorize(
    Policy =
        AuthorizationPolicies.ApplicationProcess)]
    [HttpGet("assigned-to-me")]
    public async Task<
    ActionResult<
        IReadOnlyCollection<ApplicationResponse>>>
    GetAssignedToMe(
        CancellationToken cancellationToken)
    {
        var results =
            await sender.Send(
                new GetAssignedToMeQuery(),
                cancellationToken);

        return Ok(
            results.Select(Map).ToArray());
    }

    [Authorize(
    Policy =
        AuthorizationPolicies.ApplicationProcess)]
    [HttpGet("unassigned")]
    public async Task<
    ActionResult<
        IReadOnlyCollection<ApplicationResponse>>>
    GetUnassigned(
        CancellationToken cancellationToken)
    {
        var results =
            await sender.Send(
                new GetUnassignedApplicationsQuery(),
                cancellationToken);

        return Ok(
            results.Select(Map).ToArray());
    }

    [Authorize(
    Policy =
        AuthorizationPolicies.ApplicationProcess)]
    [HttpGet("{applicationId:guid}/assignments")]
    public async Task<
    ActionResult<
        IReadOnlyCollection<
            ApplicationAssignmentResponse>>>
    GetAssignments(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var results =
            await sender.Send(
                new GetAssignmentHistoryQuery(
                    applicationId),
                cancellationToken);

        return Ok(
            results
                .Select(MapAssignment)
                .ToArray());
    }

    [Authorize(
    Policy =
        AuthorizationPolicies.ApplicationProcess)]
    [HttpGet("{applicationId:guid}/assigned")]
    public async Task<ActionResult<ApplicationResponse>>
    GetAssignedApplication(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var result =
            await sender.Send(
                new GetAssignedApplicationQuery(
                    applicationId),
                cancellationToken);

        return Ok(Map(result));
    }

    [Authorize(
    Policy =
        AuthorizationPolicies.ApplicationProcess)]
    [HttpPost(
    "{applicationId:guid}/request-information")]
    public async Task<ActionResult<ApplicationResponse>>
    RequestInformation(
        Guid applicationId,
        ProcessApplicationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryDecodeRowVersion(
                request.RowVersion,
                out var rowVersion))
        {
            ModelState.AddModelError(
                nameof(request.RowVersion),
                "RowVersion must be valid Base64.");

            return ValidationProblem(ModelState);
        }

        var result =
            await sender.Send(
                new ProcessApplicationCommand(
                    applicationId,
                    "AWAITING_INFORMATION",
                    rowVersion!,
                    request.Reason),
                cancellationToken);

        return Ok(Map(result));
    }

    [Authorize(
    Policy =
        AuthorizationPolicies.ApplicationProcess)]
    [HttpPost(
    "{applicationId:guid}/resume-processing")]
    public async Task<ActionResult<ApplicationResponse>>
    ResumeProcessing(
        Guid applicationId,
        ProcessApplicationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryDecodeRowVersion(
                request.RowVersion,
                out var rowVersion))
        {
            ModelState.AddModelError(
                nameof(request.RowVersion),
                "RowVersion must be valid Base64.");

            return ValidationProblem(ModelState);
        }

        var result =
            await sender.Send(
                new ProcessApplicationCommand(
                    applicationId,
                    "IN_PROGRESS",
                    rowVersion!,
                    request.Reason),
                cancellationToken);

        return Ok(Map(result));
    }

    [Authorize(
    Policy =
        AuthorizationPolicies.ApplicationProcess)]
    [HttpPost(
    "{applicationId:guid}/send-for-approval")]
    public async Task<ActionResult<ApplicationResponse>>
    SendForApproval(
        Guid applicationId,
        ProcessApplicationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryDecodeRowVersion(
                request.RowVersion,
                out var rowVersion))
        {
            ModelState.AddModelError(
                nameof(request.RowVersion),
                "RowVersion must be valid Base64.");

            return ValidationProblem(ModelState);
        }

        var result =
            await sender.Send(
                new ProcessApplicationCommand(
                    applicationId,
                    "AWAITING_APPROVAL",
                    rowVersion!,
                    request.Reason),
                cancellationToken);

        return Ok(Map(result));
    }

    [Authorize(
    Policy =
        AuthorizationPolicies.ApplicationApprove)]
    [HttpGet("awaiting-approval")]
    public async Task<
    ActionResult<
        IReadOnlyCollection<ApplicationResponse>>>
    GetAwaitingApproval(
        CancellationToken cancellationToken)
    {
        var results =
            await sender.Send(
                new GetAwaitingApprovalQuery(),
                cancellationToken);

        return Ok(
            results
                .Select(Map)
                .ToArray());
    }

    [Authorize(
    Policy =
        AuthorizationPolicies.ApplicationApprove)]
    [HttpPost("{applicationId:guid}/approve")]
    public async Task<ActionResult<ApplicationResponse>>
    ApproveApplication(
        Guid applicationId,
        ApplicationDecisionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryDecodeRowVersion(
                request.RowVersion,
                out var rowVersion))
        {
            ModelState.AddModelError(
                nameof(request.RowVersion),
                "RowVersion must be valid Base64.");

            return ValidationProblem(ModelState);
        }

        var result =
            await sender.Send(
                new DecideApplicationCommand(
                    applicationId,
                    "APPROVED",
                    rowVersion!,
                    request.Reason),
                cancellationToken);

        return Ok(Map(result));
    }

    [Authorize(
    Policy =
        AuthorizationPolicies.ApplicationApprove)]
    [HttpPost("{applicationId:guid}/reject")]
    public async Task<ActionResult<ApplicationResponse>>
    RejectApplication(
        Guid applicationId,
        ApplicationDecisionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryDecodeRowVersion(
                request.RowVersion,
                out var rowVersion))
        {
            ModelState.AddModelError(
                nameof(request.RowVersion),
                "RowVersion must be valid Base64.");

            return ValidationProblem(ModelState);
        }

        var result =
            await sender.Send(
                new DecideApplicationCommand(
                    applicationId,
                    "REJECTED",
                    rowVersion!,
                    request.Reason),
                cancellationToken);

        return Ok(Map(result));
    }

    private static ApplicationResponse Map(
        ApplicationResult result)
    {
        return new ApplicationResponse(
            result.ApplicationId,
            result.ApplicationNumber,
            result.ApplicationType,
            result.ApplicantCitizenId,
            result.SubjectCitizenId,
            result.Status,
            result.IsSelfApplication,
            result.SubmittedAtUtc,
            result.CreatedAtUtc,
            result.UpdatedAtUtc,
            Convert.ToBase64String(
                result.RowVersion));
    }

    private static bool TryDecodeRowVersion(
    string value,
    out byte[]? rowVersion)
    {
        rowVersion = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            rowVersion =
                Convert.FromBase64String(value);

            return rowVersion.Length == 8;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static ApplicationAssignmentResponse
    MapAssignment(
        ApplicationAssignmentResult result)
    {
        return new ApplicationAssignmentResponse(
            result.ApplicationAssignmentId,
            result.ApplicationId,
            result.AssignedToUserId,
            result.AssignedByUserId,
            result.AssignedAtUtc,
            result.UnassignedAtUtc,
            result.UnassignedByUserId,
            result.Reason,
            result.IsActive,
            Convert.ToBase64String(
                result.RowVersion));
    }
}
