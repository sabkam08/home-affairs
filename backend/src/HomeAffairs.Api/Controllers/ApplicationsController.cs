using HomeAffairs.Api.Authorization;
using HomeAffairs.Application.Applications;
using HomeAffairs.Application.Applications.CreateApplication;
using HomeAffairs.Application.Applications.GetApplication;
using HomeAffairs.Application.Applications.GetMyApplications;
using HomeAffairs.Application.Applications.UpdateApplication;
using HomeAffairs.Application.Applications.SubmitApplication;
using HomeAffairs.Application.Applications.CancelApplication;
using HomeAffairs.Application.Applications.GetApplicationHistory;
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
}
