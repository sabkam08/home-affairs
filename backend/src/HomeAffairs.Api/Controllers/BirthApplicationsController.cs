using HomeAffairs.Api.Authorization;
using HomeAffairs.Application.Birth;
using HomeAffairs.Application.Birth.GetBirthServiceDetails;
using HomeAffairs.Application.Birth.SaveBirthServiceDetails;
using HomeAffairs.Contracts.Birth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeAffairs.Api.Controllers;

[ApiController]
[Authorize]
[Route(
    "api/v1/applications/{applicationId:guid}/birth")]
public sealed class BirthApplicationsController(
    ISender sender)
    : ControllerBase
{
    [HttpGet]
    public async Task<
        ActionResult<BirthServiceDetailsResponse>>
        Get(
            Guid applicationId,
            CancellationToken cancellationToken)
    {
        var result =
            await sender.Send(
                new GetBirthServiceDetailsQuery(
                    applicationId),
                cancellationToken);

        return Ok(Map(result));
    }

    [Authorize(
        Policy =
            AuthorizationPolicies.ApplicationCreate)]
    [HttpPut]
    public async Task<
        ActionResult<BirthServiceDetailsResponse>>
        Save(
            Guid applicationId,
            SaveBirthServiceDetailsRequest request,
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
                new SaveBirthServiceDetailsCommand(
                    applicationId,
                    request.ChildFirstName,
                    request.ChildMiddleNames,
                    request.ChildLastName,
                    request.ChildDateOfBirth,
                    request.ChildSex,
                    request.PlaceOfBirth,
                    request.ProvinceOfBirth,
                    request.CountryOfBirth,
                    request.MotherFirstName,
                    request.MotherLastName,
                    request.MotherIdNumber,
                    request.FatherFirstName,
                    request.FatherLastName,
                    request.FatherIdNumber,
                    request.CurrentCertificateNumber,
                    request.RequestReason,
                    rowVersion),
                cancellationToken);

        return Ok(Map(result));
    }

    private static BirthServiceDetailsResponse Map(
        BirthServiceDetailsResult result)
    {
        return new BirthServiceDetailsResponse(
            result.BirthServiceDetailsId,
            result.ApplicationId,
            result.ApplicationType,
            result.ChildFirstName,
            result.ChildMiddleNames,
            result.ChildLastName,
            result.ChildDateOfBirth,
            result.ChildSex,
            result.PlaceOfBirth,
            result.ProvinceOfBirth,
            result.CountryOfBirth,
            result.MotherFirstName,
            result.MotherLastName,
            result.MotherIdNumber,
            result.FatherFirstName,
            result.FatherLastName,
            result.FatherIdNumber,
            result.CurrentCertificateNumber,
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
                Convert.FromBase64String(value);

            return rowVersion.Length == 8;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
