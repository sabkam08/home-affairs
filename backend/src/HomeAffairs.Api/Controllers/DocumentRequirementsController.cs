using HomeAffairs.Application.Documents.GetDocumentReadiness;
using HomeAffairs.Application.Documents.GetDocumentRequirements;
using HomeAffairs.Contracts.Documents;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeAffairs.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/document-requirements")]
public sealed class DocumentRequirementsController(
    ISender sender)
    : ControllerBase
{
    [HttpGet("{applicationType}")]
    public async Task<
        ActionResult<
            IReadOnlyCollection<
                DocumentRequirementResponse>>>
        GetRequirements(
            string applicationType,
            CancellationToken cancellationToken)
    {
        var results =
            await sender.Send(
                new GetDocumentRequirementsQuery(
                    applicationType),
                cancellationToken);

        return Ok(
            results
                .Select(result =>
                    new DocumentRequirementResponse(
                        result.DocumentType,
                        result.DisplayName,
                        result.Description,
                        result.IsRequired,
                        result.MinimumCount))
                .ToArray());
    }

    [HttpGet(
        "/api/v1/applications/{applicationId:guid}/document-readiness")]
    public async Task<
        ActionResult<DocumentReadinessResponse>>
        GetReadiness(
            Guid applicationId,
            CancellationToken cancellationToken)
    {
        var result =
            await sender.Send(
                new GetDocumentReadinessQuery(
                    applicationId),
                cancellationToken);

        return Ok(
            new DocumentReadinessResponse(
                result.ApplicationId,
                result.IsReady,
                result.RequiredCount,
                result.SatisfiedCount,
                result.Requirements
                    .Select(requirement =>
                        new DocumentRequirementStatusResponse(
                            requirement.DocumentType,
                            requirement.DisplayName,
                            requirement.Description,
                            requirement.MinimumCount,
                            requirement.UploadedCount,
                            requirement.IsSatisfied))
                    .ToArray()));
    }
}
