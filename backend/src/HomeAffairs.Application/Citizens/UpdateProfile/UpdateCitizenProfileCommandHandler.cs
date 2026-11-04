using HomeAffairs.Application.Abstractions.Citizens;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Domain.Common.Exceptions;
using HomeAffairs.Domain.Citizens;
using MediatR;

namespace HomeAffairs.Application.Citizens.UpdateProfile;

internal sealed class UpdateCitizenProfileCommandHandler(
    ICitizenRepository citizenRepository,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<
        UpdateCitizenProfileCommand,
        UpdateCitizenProfileResult>
{
    public async Task<UpdateCitizenProfileResult> Handle(
        UpdateCitizenProfileCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        var existing =
            await citizenRepository.GetByUserIdAsync(
                currentUser.UserId.Value,
                cancellationToken);

        if (existing is null)
        {
            throw new CitizenProfileNotFoundException();
        }

        var updatedCitizen =
            new Citizen
            {
                CitizenId =
                    existing.CitizenId,

                UserId =
                    existing.UserId,

                SouthAfricanIdNumber =
                    NormalizeNullable(
                        request.SouthAfricanIdNumber),

                FirstName =
                    request.FirstName.Trim(),

                MiddleNames =
                    NormalizeNullable(
                        request.MiddleNames),

                LastName =
                    request.LastName.Trim(),

                DateOfBirth =
                    request.DateOfBirth,

                Sex =
                    request.Sex
                        .Trim()
                        .ToUpperInvariant(),

                IsActive =
                    existing.IsActive,

                CreatedAtUtc =
                    existing.CreatedAtUtc,

                UpdatedAtUtc =
                    clock.UtcNow,

                RowVersion =
                    existing.RowVersion
            };

        try
        {
            var persisted =
                await citizenRepository
                    .UpdateOwnProfileAsync(
                        updatedCitizen,
                        request.ExpectedRowVersion,
                        cancellationToken);

            return Map(persisted);
        }
        catch (CitizenConcurrencyException)
        {
            throw new CitizenProfileConcurrencyException();
        }
    }

    private static string? NormalizeNullable(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static UpdateCitizenProfileResult Map(
        Citizen citizen)
    {
        return new UpdateCitizenProfileResult(
            citizen.CitizenId,
            citizen.SouthAfricanIdNumber,
            citizen.FirstName,
            citizen.MiddleNames,
            citizen.LastName,
            citizen.DateOfBirth,
            citizen.Sex,
            citizen.IsActive,
            citizen.CreatedAtUtc,
            citizen.UpdatedAtUtc,
            citizen.RowVersion);
    }
}
