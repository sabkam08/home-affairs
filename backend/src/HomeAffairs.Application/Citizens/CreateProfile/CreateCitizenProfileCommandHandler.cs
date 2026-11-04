using HomeAffairs.Application.Abstractions.Citizens;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Domain.Common.Exceptions;
using HomeAffairs.Domain.Citizens;
using MediatR;

namespace HomeAffairs.Application.Citizens.CreateProfile;

internal sealed class CreateCitizenProfileCommandHandler(
    ICitizenRepository citizenRepository,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<
        CreateCitizenProfileCommand,
        CreateCitizenProfileResult>
{
    public async Task<CreateCitizenProfileResult> Handle(
        CreateCitizenProfileCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        var existingCitizen =
            await citizenRepository.GetByUserIdAsync(
                currentUser.UserId.Value,
                cancellationToken);

        if (existingCitizen is not null)
        {
            throw new CitizenProfileAlreadyExistsException();
        }

        var citizen = new Citizen
        {
            CitizenId = Guid.NewGuid(),
            UserId = currentUser.UserId.Value,

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

            IsActive = true,

            CreatedAtUtc =
                clock.UtcNow
        };

        var created =
            await citizenRepository.CreateForUserAsync(
                citizen,
                cancellationToken);

        return Map(created);
    }

    private static string? NormalizeNullable(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static CreateCitizenProfileResult Map(
        Citizen citizen)
    {
        return new CreateCitizenProfileResult(
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
