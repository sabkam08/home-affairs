using HomeAffairs.Api.Authorization;
using HomeAffairs.Api.Models.Documents;
using HomeAffairs.Application.Documents;
using HomeAffairs.Application.Documents.GetApplicationDocuments;
using HomeAffairs.Application.Documents.GetDocumentContent;
using HomeAffairs.Application.Documents.UploadDocument;
using HomeAffairs.Application.Documents.CreateDocumentVersion;
using HomeAffairs.Application.Documents.DeactivateDocument;
using HomeAffairs.Application.Documents.GetDocumentVersions;
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

    [Authorize(
    Policy =
        AuthorizationPolicies.ApplicationCreate)]
    [HttpPost("{documentId:guid}/versions")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(
    SupportedDocumentFormats.MaximumFileSizeBytes)]
    [ProducesResponseType(
    typeof(DocumentResponse),
    StatusCodes.Status201Created)]
    [ProducesResponseType(
    typeof(ValidationProblemDetails),
    StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status404NotFound)]
    [ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DocumentResponse>>
    CreateVersion(
        Guid applicationId,
        Guid documentId,
        [FromForm] CreateDocumentVersionForm form,
        CancellationToken cancellationToken)
    {
        if (!TryDecodeRowVersion(
                form.RowVersion,
                out var rowVersion))
        {
            ModelState.AddModelError(
                nameof(form.RowVersion),
                "RowVersion must be valid Base64.");

            return ValidationProblem(ModelState);
        }

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

        await using var memory =
            new MemoryStream();

        await stream.CopyToAsync(
            memory,
            cancellationToken);

        var result =
            await sender.Send(
                new CreateDocumentVersionCommand(
                    applicationId,
                    documentId,
                    form.File.FileName,
                    form.File.ContentType,
                    memory.ToArray(),
                    rowVersion!),
                cancellationToken);

        return Created(
            $"/api/v1/applications/" +
            $"{applicationId}/documents/" +
            $"{result.DocumentId}",
            Map(result));
    }

    [HttpGet("{documentId:guid}/versions")]
    [ProducesResponseType(
    typeof(IReadOnlyCollection<DocumentResponse>),
    StatusCodes.Status200OK)]
    [ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status404NotFound)]
    public async Task<
    ActionResult<IReadOnlyCollection<DocumentResponse>>>
    GetVersions(
        Guid applicationId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var results =
            await sender.Send(
                new GetDocumentVersionsQuery(
                    applicationId,
                    documentId),
                cancellationToken);

        return Ok(
            results
                .Select(Map)
                .ToArray());
    }

    [Authorize(
    Policy =
        AuthorizationPolicies.ApplicationCreate)]
    [HttpDelete("{documentId:guid}")]
    [ProducesResponseType(
    StatusCodes.Status204NoContent)]
    [ProducesResponseType(
    typeof(ValidationProblemDetails),
    StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status404NotFound)]
    [ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status409Conflict)]
    public async Task<IActionResult>
    Delete(
        Guid applicationId,
        Guid documentId,
        DeleteDocumentRequest request,
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

        await sender.Send(
            new DeactivateDocumentCommand(
                applicationId,
                documentId,
                rowVersion!),
            cancellationToken);

        return NoContent();
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
