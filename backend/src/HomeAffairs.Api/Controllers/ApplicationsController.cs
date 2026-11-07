using HomeAffairs.Api.Authorization;
using HomeAffairs.Application.Applications;
using HomeAffairs.Application.Applications.CreateApplication;
using HomeAffairs.Application.Applications.GetApplication;
using HomeAffairs.Application.Applications.GetMyApplications;
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
}
