using HomeAffairs.Api.Authorization;
using HomeAffairs.Application.Citizens.CreateProfile;
using HomeAffairs.Application.Citizens.GetMyProfile;
using HomeAffairs.Application.Citizens.UpdateProfile;
using HomeAffairs.Application.Citizens.ContactDetails;
using HomeAffairs.Application.Citizens.Addresses;
using HomeAffairs.Application.Citizens.Dependants;
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

    [Authorize(
    Policy =
        AuthorizationPolicies.ProfileManageSelf)]
    [HttpPut(
    "me/contacts/{contactDetailId:guid}")]
    [ProducesResponseType(
    typeof(ContactDetailResponse),
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
    ActionResult<ContactDetailResponse>>
    UpdateContactDetail(
        Guid contactDetailId,
        UpdateContactDetailRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryDecodeRowVersion(
                request.RowVersion,
                out var expectedRowVersion))
        {
            ModelState.AddModelError(
                nameof(request.RowVersion),
                "RowVersion must be valid Base64.");

            return ValidationProblem(ModelState);
        }

        var result =
            await sender.Send(
                new UpdateContactDetailCommand(
                    contactDetailId,
                    request.ContactType,
                    request.ContactValue,
                    request.IsPrimary,
                    expectedRowVersion!),
                cancellationToken);

        return Ok(
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
                result.RowVersion));
    }

    [Authorize(
    Policy =
        AuthorizationPolicies.ProfileManageSelf)]
    [HttpDelete(
    "me/contacts/{contactDetailId:guid}")]
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
    DeleteContactDetail(
        Guid contactDetailId,
        DeleteContactDetailRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryDecodeRowVersion(
                request.RowVersion,
                out var expectedRowVersion))
        {
            ModelState.AddModelError(
                nameof(request.RowVersion),
                "RowVersion must be valid Base64.");

            return ValidationProblem(ModelState);
        }

        await sender.Send(
            new DeleteContactDetailCommand(
                contactDetailId,
                expectedRowVersion!),
            cancellationToken);

        return NoContent();
    }

    [Authorize(
    Policy =
        AuthorizationPolicies.ProfileManageSelf)]
    [HttpPost("me/addresses")]
    public async Task<ActionResult<AddressResponse>>
    CreateAddress(
        CreateAddressRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await sender.Send(
                new CreateAddressCommand(
                    request.AddressType,
                    request.AddressLine1,
                    request.AddressLine2,
                    request.Suburb,
                    request.CityOrTown,
                    request.Province,
                    request.PostalCode,
                    request.CountryCode,
                    request.IsPrimary),
                cancellationToken);

        var response =
            MapAddressResponse(result);

        return Created(
            $"/api/v1/citizens/me/addresses/" +
            $"{result.AddressId}",
            response);
    }

    [Authorize(
    Policy =
        AuthorizationPolicies.ProfileReadSelf)]
    [HttpGet("me/addresses")]
    public async Task<
    ActionResult<IReadOnlyCollection<AddressResponse>>>
    GetAddresses(
        CancellationToken cancellationToken)
    {
        var results =
            await sender.Send(
                new GetMyAddressesQuery(),
                cancellationToken);

        return Ok(
            results
                .Select(MapAddressResponse)
                .ToArray());
    }

    [Authorize(
    Policy =
        AuthorizationPolicies.ProfileManageSelf)]
    [HttpPut("me/addresses/{addressId:guid}")]
    public async Task<ActionResult<AddressResponse>>
    UpdateAddress(
        Guid addressId,
        UpdateAddressRequest request,
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
                new UpdateAddressCommand(
                    addressId,
                    request.AddressType,
                    request.AddressLine1,
                    request.AddressLine2,
                    request.Suburb,
                    request.CityOrTown,
                    request.Province,
                    request.PostalCode,
                    request.CountryCode,
                    request.IsPrimary,
                    rowVersion!),
                cancellationToken);

        return Ok(MapAddressResponse(result));
    }

    [Authorize(
    Policy =
        AuthorizationPolicies.ProfileManageSelf)]
    [HttpDelete("me/addresses/{addressId:guid}")]
    public async Task<IActionResult>
    DeleteAddress(
        Guid addressId,
        DeleteAddressRequest request,
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
            new DeleteAddressCommand(
                addressId,
                rowVersion!),
            cancellationToken);

        return NoContent();
    }

    [Authorize(
    Policy =
        AuthorizationPolicies.ProfileManageSelf)]
    [HttpPost("me/dependants")]
    public async Task<ActionResult<DependantResponse>>
    CreateDependant(
        CreateDependantRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await sender.Send(
                new CreateDependantCommand(
                    request.SouthAfricanIdNumber,
                    request.FirstName,
                    request.MiddleNames,
                    request.LastName,
                    request.DateOfBirth,
                    request.Sex,
                    request.RelationshipType),
                cancellationToken);

        var response =
            MapDependantResponse(result);

        return Created(
            $"/api/v1/citizens/me/dependants/" +
            $"{result.SubjectCitizenId}",
            response);
    }

    [Authorize(
        Policy =
            AuthorizationPolicies.ProfileReadSelf)]
    [HttpGet("me/dependants")]
    public async Task<
        ActionResult<
            IReadOnlyCollection<DependantResponse>>>
        GetDependants(
            CancellationToken cancellationToken)
    {
        var results =
            await sender.Send(
                new GetMyDependantsQuery(),
                cancellationToken);

        return Ok(
            results
                .Select(MapDependantResponse)
                .ToArray());
    }

    [Authorize(
        Policy =
            AuthorizationPolicies.ProfileReadSelf)]
    [HttpGet(
        "me/dependants/{subjectCitizenId:guid}")]
    public async Task<ActionResult<DependantResponse>>
        GetDependant(
            Guid subjectCitizenId,
            CancellationToken cancellationToken)
    {
        var result =
            await sender.Send(
                new GetMyDependantQuery(
                    subjectCitizenId),
                cancellationToken);

        return Ok(
            MapDependantResponse(result));
    }

    private static DependantResponse
        MapDependantResponse(
            DependantResult result)
    {
        return new DependantResponse(
            result.CitizenRelationshipId,
            result.RepresentativeCitizenId,
            result.SubjectCitizenId,
            result.RelationshipType,
            result.SouthAfricanIdNumber,
            result.FirstName,
            result.MiddleNames,
            result.LastName,
            result.DateOfBirth,
            result.Sex,
            result.IsMinor,
            result.IsActive,
            result.CreatedAtUtc,
            Convert.ToBase64String(
                result.RelationshipRowVersion),
            Convert.ToBase64String(
                result.SubjectRowVersion));
    }

    [Authorize(
    Policy =
        AuthorizationPolicies.ProfileManageSelf)]
    [HttpPut(
    "me/dependants/{subjectCitizenId:guid}/relationship")]
    [ProducesResponseType(
    typeof(DependantResponse),
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
    public async Task<ActionResult<DependantResponse>>
    UpdateDependantRelationship(
        Guid subjectCitizenId,
        UpdateDependantRelationshipRequest request,
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
                new UpdateDependantRelationshipCommand(
                    subjectCitizenId,
                    request.RelationshipType,
                    rowVersion!),
                cancellationToken);

        return Ok(
            MapDependantResponse(result));
    }

    [Authorize(
    Policy =
        AuthorizationPolicies.ProfileManageSelf)]
    [HttpDelete(
    "me/dependants/{subjectCitizenId:guid}/relationship")]
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
    DeactivateDependantRelationship(
        Guid subjectCitizenId,
        DeleteDependantRelationshipRequest request,
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
            new DeactivateDependantRelationshipCommand(
                subjectCitizenId,
                rowVersion!),
            cancellationToken);

        return NoContent();
    }



    private static AddressResponse
    MapAddressResponse(
        CitizenAddressResult result)
    {
        return new AddressResponse(
            result.AddressId,
            result.CitizenId,
            result.AddressType,
            result.AddressLine1,
            result.AddressLine2,
            result.Suburb,
            result.CityOrTown,
            result.Province,
            result.PostalCode,
            result.CountryCode,
            result.IsPrimary,
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
