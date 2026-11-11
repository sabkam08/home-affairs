using HomeAffairs.Api.Authorization;
using HomeAffairs.Api.Models.Documents;
using HomeAffairs.Application.Documents;
using HomeAffairs.Application.Documents.GetApplicationDocuments;
using HomeAffairs.Application.Documents.GetDocumentContent;
using HomeAffairs.Application.Documents.UploadDocument;
using HomeAffairs.Contracts.Documents;
using HomeAffairs.Domain.Documents;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeAffairs.Api.Controllers;

[ApiController]
[Authorize]
[Route(
    "api/v1/applications/{applicationId:guid}/documents")]
public sealed class DocumentsController(
    ISender sender)
    : ControllerBase
{
    [Authorize(
        Policy =
            AuthorizationPolicies.ApplicationCreate)]
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(
        SupportedDocumentFormats.MaximumFileSizeBytes)]
    [ProducesResponseType(
        typeof(DocumentResponse),
        StatusCodes.Status201Created)]
    public async Task<ActionResult<DocumentResponse>>
        Upload(
            Guid applicationId,
            [FromForm] UploadDocumentForm form,
            CancellationToken cancellationToken)
    {
        if (form.File is null ||
            form.File.Length <= 0)
        {
            return BadRequest(
                new ProblemDetails
                {
                    Status =
                        StatusCodes.Status400BadRequest,
                    Title =
                        "Invalid document",
                    Detail =
                        "A document file is required."
                });
        }

        if (form.File.Length >
            SupportedDocumentFormats.MaximumFileSizeBytes)
        {
            return BadRequest(
                new ProblemDetails
                {
                    Status =
                        StatusCodes.Status400BadRequest,
                    Title =
                        "Invalid document",
                    Detail =
                        "The document exceeds the maximum " +
                        "allowed size of 15 MB."
                });
        }

        await using var stream =
            form.File.OpenReadStream();

        await using var memoryStream =
            new MemoryStream();

        await stream.CopyToAsync(
            memoryStream,
            cancellationToken);

        var result =
            await sender.Send(
                new UploadDocumentCommand(
                    applicationId,
                    form.DocumentType,
                    form.File.FileName,
                    form.File.ContentType,
                    memoryStream.ToArray()),
                cancellationToken);

        var response =
            Map(result);

        return Created(
            $"/api/v1/applications/" +
            $"{applicationId}/documents/" +
            $"{result.DocumentId}",
            response);
    }

    [HttpGet]
    public async Task<
        ActionResult<
            IReadOnlyCollection<DocumentResponse>>>
        GetDocuments(
            Guid applicationId,
            CancellationToken cancellationToken)
    {
        var results =
            await sender.Send(
                new GetApplicationDocumentsQuery(
                    applicationId),
                cancellationToken);

        return Ok(
            results
                .Select(Map)
                .ToArray());
    }

    [HttpGet("{documentId:guid}/content")]
    public async Task<IActionResult>
        Download(
            Guid applicationId,
            Guid documentId,
            CancellationToken cancellationToken)
    {
        var result =
            await sender.Send(
                new GetDocumentContentQuery(
                    applicationId,
                    documentId),
                cancellationToken);

        return File(
            result.Content,
            result.ContentType,
            result.FileName);
    }

    private static DocumentResponse Map(
        DocumentResult result)
    {
        return new DocumentResponse(
            result.DocumentId,
            result.LogicalDocumentId,
            result.VersionNumber,
            result.ApplicationId,
            result.DocumentType,
            result.FileName,
            result.ContentType,
            result.FileExtension,
            result.FileSizeBytes,
            result.ContentHash,
            result.UploadedByUserId,
            result.IsActive,
            result.CreatedAtUtc,
            result.UpdatedAtUtc,
            
            Convert.ToBase64String(
                result.RowVersion));
    }
}
