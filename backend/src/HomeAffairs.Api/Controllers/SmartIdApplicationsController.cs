using HomeAffairs.Api.Authorization;
using HomeAffairs.Application.SmartId;
using HomeAffairs.Application.SmartId.GetSmartIdServiceDetails;
using HomeAffairs.Application.SmartId.SaveSmartIdServiceDetails;
using HomeAffairs.Contracts.SmartId;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeAffairs.Api.Controllers;

[ApiController]
[Authorize]
[Route(
    "api/v1/applications/{applicationId:guid}/smart-id")]
public sealed class SmartIdApplicationsController(
    ISender sender)
    : ControllerBase
{
    [HttpGet]
    public async Task<
        ActionResult<SmartIdServiceDetailsResponse>>
        Get(
            Guid applicationId,
            CancellationToken cancellationToken)
    {
        var result =
            await sender.Send(
                new GetSmartIdServiceDetailsQuery(
                    applicationId),
                cancellationToken);

        return Ok(Map(result));
    }

    [Authorize(
        Policy =
            AuthorizationPolicies.ApplicationCreate)]
    [HttpPut]
    public async Task<
        ActionResult<SmartIdServiceDetailsResponse>>
        Save(
            Guid applicationId,
            SaveSmartIdServiceDetailsRequest request,
            CancellationToken cancellationToken)
    {
        byte[]? rowVersion =
            null;

        if (!string.IsNullOrWhiteSpace(
                request.RowVersion))
        {
            if (!TryDecodeRowVersion(
                    request.RowVersion,
                    out rowVersion))
            {
                ModelState.AddModelError(
                    nameof(request.RowVersion),
                    "RowVersion must be valid Base64.");

                return ValidationProblem(
                    ModelState);
            }
        }

        var result =
            await sender.Send(
                new SaveSmartIdServiceDetailsCommand(
                    applicationId,
                    request.ExistingSmartIdNumber,
                    request.RequestedFirstName,
                    request.RequestedMiddleNames,
                    request.RequestedLastName,
                    request.RequestedDateOfBirth,
                    request.RequestedSex,
                    request.ReplacementReason,
                    request.IncidentDate,
                    request.PoliceCaseNumber,
                    request.PoliceStation,
                    request.DamageDescription,
                    request.CorrectionField,
                    request.CurrentValue,
                    request.RequestedValue,
                    request.RequestReason,
                    rowVersion),
                cancellationToken);

        return Ok(Map(result));
    }

    private static SmartIdServiceDetailsResponse Map(
        SmartIdServiceDetailsResult result)
    {
        return new SmartIdServiceDetailsResponse(
            result.SmartIdServiceDetailsId,
            result.ApplicationId,
            result.ApplicationType,
            result.ExistingSmartIdNumber,
            result.RequestedFirstName,
            result.RequestedMiddleNames,
            result.RequestedLastName,
            result.RequestedDateOfBirth,
            result.RequestedSex,
            result.ReplacementReason,
            result.IncidentDate,
            result.PoliceCaseNumber,
            result.PoliceStation,
            result.DamageDescription,
            result.CorrectionField,
            result.CurrentValue,
            result.RequestedValue,
            result.RequestReason,
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

        try
        {
            rowVersion =
                Convert.FromBase64String(
                    value);

            return rowVersion.Length == 8;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
