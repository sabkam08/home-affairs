using HomeAffairs.Api.Authorization;
using HomeAffairs.Application.Citizens.CreateProfile;
using HomeAffairs.Application.Citizens.GetMyProfile;
using HomeAffairs.Application.Citizens.UpdateProfile;
using HomeAffairs.Application.Citizens.ContactDetails;
using HomeAffairs.Contracts.Citizens;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeAffairs.Api.Controllers;

[ApiController]
[Route("api/v1/citizens")]
public sealed class CitizensController(
    ISender sender)
    : ControllerBase
{
    [Authorize(
        Policy =
            AuthorizationPolicies.ProfileManageSelf)]
    [HttpPost("me")]
    [ProducesResponseType(
        typeof(CitizenProfileResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<
        ActionResult<CitizenProfileResponse>>
        CreateMyProfile(
            CreateCitizenProfileRequest request,
            CancellationToken cancellationToken)
    {
        var result =
            await sender.Send(
                new CreateCitizenProfileCommand(
                    request.SouthAfricanIdNumber,
                    request.FirstName,
                    request.MiddleNames,
                    request.LastName,
                    request.DateOfBirth,
                    request.Sex),
                cancellationToken);

        var response =
            MapResponse(
                result.CitizenId,
                result.SouthAfricanIdNumber,
                result.FirstName,
                result.MiddleNames,
                result.LastName,
                result.DateOfBirth,
                result.Sex,
                result.IsActive,
                result.CreatedAtUtc,
                result.UpdatedAtUtc,
                result.RowVersion);

        return Created(
            "/api/v1/citizens/me",
            response);
    }

    [Authorize(
        Policy =
            AuthorizationPolicies.ProfileReadSelf)]
    [HttpGet("me")]
    [ProducesResponseType(
        typeof(CitizenProfileResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<
        ActionResult<CitizenProfileResponse>>
        GetMyProfile(
            CancellationToken cancellationToken)
    {
        var result =
            await sender.Send(
                new GetMyCitizenProfileQuery(),
                cancellationToken);

        return Ok(
            MapResponse(
                result.CitizenId,
                result.SouthAfricanIdNumber,
                result.FirstName,
                result.MiddleNames,
                result.LastName,
                result.DateOfBirth,
                result.Sex,
                result.IsActive,
                result.CreatedAtUtc,
                result.UpdatedAtUtc,
                result.RowVersion));
    }

    private static CitizenProfileResponse MapResponse(
        Guid citizenId,
        string? southAfricanIdNumber,
        string firstName,
        string? middleNames,
        string lastName,
        DateOnly dateOfBirth,
        string sex,
        bool isActive,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? updatedAtUtc,
        byte[] rowVersion)
    {
        return new CitizenProfileResponse(
            citizenId,
            southAfricanIdNumber,
            firstName,
            middleNames,
            lastName,
            dateOfBirth,
            sex,
            isActive,
            createdAtUtc,
            updatedAtUtc,
            Convert.ToBase64String(
                rowVersion));
    }

    [Authorize(
    Policy =
        AuthorizationPolicies.ProfileManageSelf)]
    [HttpPut("me")]
    [ProducesResponseType(
    typeof(CitizenProfileResponse),
    StatusCodes.Status200OK)]
    [ProducesResponseType(
    typeof(ValidationProblemDetails),
    StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status404NotFound)]
    [ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status409Conflict)]
    public async Task<
    ActionResult<CitizenProfileResponse>>
    UpdateMyProfile(
        UpdateCitizenProfileRequest request,
        CancellationToken cancellationToken)
    {
        byte[] expectedRowVersion;

        try
        {
            expectedRowVersion =
                Convert.FromBase64String(
                    request.RowVersion);
        }
        catch (FormatException)
        {
            ModelState.AddModelError(
                nameof(request.RowVersion),
                "RowVersion must be valid Base64.");

            return ValidationProblem(ModelState);
        }

        var result =
            await sender.Send(
                new UpdateCitizenProfileCommand(
                    request.SouthAfricanIdNumber,
                    request.FirstName,
                    request.MiddleNames,
                    request.LastName,
                    request.DateOfBirth,
                    request.Sex,
                    expectedRowVersion),
                cancellationToken);

        return Ok(
            MapResponse(
                result.CitizenId,
                result.SouthAfricanIdNumber,
                result.FirstName,
                result.MiddleNames,
                result.LastName,
                result.DateOfBirth,
                result.Sex,
                result.IsActive,
                result.CreatedAtUtc,
                result.UpdatedAtUtc,
                result.RowVersion));
    }

    [Authorize(
    Policy =
        AuthorizationPolicies.ProfileManageSelf)]
    [HttpPost("me/contacts")]
    [ProducesResponseType(
    typeof(ContactDetailResponse),
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
    public async Task<
    ActionResult<ContactDetailResponse>>
    CreateContactDetail(
        CreateContactDetailRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await sender.Send(
                new CreateContactDetailCommand(
                    request.ContactType,
                    request.ContactValue,
                    request.IsPrimary),
                cancellationToken);

        var response =
            MapContactResponse(
                result.ContactDetailId,
                result.CitizenId,
                result.ContactType,
                result.ContactValue,
                result.IsPrimary,
                result.IsVerified,
                result.IsActive,
                result.CreatedAtUtc,
                result.UpdatedAtUtc,
                result.RowVersion);

        return Created(
            $"/api/v1/citizens/me/contacts/" +
            $"{response.ContactDetailId}",
            response);
    }

    [Authorize(
        Policy =
            AuthorizationPolicies.ProfileReadSelf)]
    [HttpGet("me/contacts")]
    [ProducesResponseType(
        typeof(IReadOnlyCollection<
            ContactDetailResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<
        ActionResult<
            IReadOnlyCollection<
                ContactDetailResponse>>>
        GetContactDetails(
            CancellationToken cancellationToken)
    {
        var results =
            await sender.Send(
                new GetMyContactDetailsQuery(),
                cancellationToken);

        var response =
            results
                .Select(result =>
                    MapContactResponse(
                        result.ContactDetailId,
                        result.CitizenId,
                        result.ContactType,
                        result.ContactValue,
                        result.IsPrimary,
                        result.IsVerified,
                        result.IsActive,
                        result.CreatedAtUtc,
                        result.UpdatedAtUtc,
                        result.RowVersion))
                .ToArray();

        return Ok(response);
    }

    private static ContactDetailResponse
        MapContactResponse(
            Guid contactDetailId,
            Guid citizenId,
            string contactType,
            string contactValue,
            bool isPrimary,
            bool isVerified,
            bool isActive,
            DateTimeOffset createdAtUtc,
            DateTimeOffset? updatedAtUtc,
            byte[] rowVersion)
    {
        return new ContactDetailResponse(
            contactDetailId,
            citizenId,
            contactType,
            contactValue,
            isPrimary,
            isVerified,
            isActive,
            createdAtUtc,
            updatedAtUtc,
            Convert.ToBase64String(rowVersion));
    }
}
