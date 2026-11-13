using HomeAffairs.Application.Abstractions.Birth;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Domain.Birth;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;

namespace HomeAffairs.Application.Birth.SaveBirthServiceDetails;

internal sealed class SaveBirthServiceDetailsCommandHandler(
    IBirthServiceRepository repository,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<
        SaveBirthServiceDetailsCommand,
        BirthServiceDetailsResult>
{
    public async Task<BirthServiceDetailsResult> Handle(
        SaveBirthServiceDetailsCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        var details =
            new BirthServiceDetails
            {
                BirthServiceDetailsId =
                    Guid.NewGuid(),

                ApplicationId =
                    request.ApplicationId,

                ChildFirstName =
                    Normalize(request.ChildFirstName),

                ChildMiddleNames =
                    Normalize(request.ChildMiddleNames),

                ChildLastName =
                    Normalize(request.ChildLastName),

                ChildDateOfBirth =
                    request.ChildDateOfBirth,

                ChildSex =
                    NormalizeUpper(
                        request.ChildSex),

                PlaceOfBirth =
                    Normalize(request.PlaceOfBirth),

                ProvinceOfBirth =
                    Normalize(request.ProvinceOfBirth),

                CountryOfBirth =
                    Normalize(request.CountryOfBirth),

                MotherFirstName =
                    Normalize(request.MotherFirstName),

                MotherLastName =
                    Normalize(request.MotherLastName),

                MotherIdNumber =
                    Normalize(request.MotherIdNumber),

                FatherFirstName =
                    Normalize(request.FatherFirstName),

                FatherLastName =
                    Normalize(request.FatherLastName),

                FatherIdNumber =
                    Normalize(request.FatherIdNumber),

                CurrentCertificateNumber =
                    Normalize(
                        request.CurrentCertificateNumber),

                RequestReason =
                    Normalize(request.RequestReason),

                CreatedAtUtc =
                    clock.UtcNow
            };

        try
        {
            var saved =
                await repository.SaveForApplicantAsync(
                    details,
                    currentUser.UserId.Value,
                    request.ExpectedRowVersion,
                    clock.UtcNow,
                    cancellationToken);

            return Map(saved);
        }
        catch (
            BirthApplicationNotFoundPersistenceException)
        {
            throw new
                BirthServiceApplicationNotFoundException();
        }
        catch (
            InvalidBirthApplicationTypePersistenceException)
        {
            throw new
                InvalidBirthServiceApplicationException();
        }
        catch (
            BirthDetailsNotEditablePersistenceException)
        {
            throw new
                BirthServiceDetailsNotEditableException();
        }
        catch (
            BirthDetailsConcurrencyPersistenceException)
        {
            throw new
                BirthServiceDetailsConflictException();
        }
    }

    internal static BirthServiceDetailsResult Map(
        BirthServiceDetails details)
    {
        return new BirthServiceDetailsResult(
            details.BirthServiceDetailsId,
            details.ApplicationId,
            details.ApplicationType,
            details.ChildFirstName,
            details.ChildMiddleNames,
            details.ChildLastName,
            details.ChildDateOfBirth,
            details.ChildSex,
            details.PlaceOfBirth,
            details.ProvinceOfBirth,
            details.CountryOfBirth,
            details.MotherFirstName,
            details.MotherLastName,
            details.MotherIdNumber,
            details.FatherFirstName,
            details.FatherLastName,
            details.FatherIdNumber,
            details.CurrentCertificateNumber,
            details.RequestReason,
            details.CreatedAtUtc,
            details.UpdatedAtUtc,
            details.RowVersion);
    }

    private static string? Normalize(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string? NormalizeUpper(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value
                .Trim()
                .ToUpperInvariant();
    }
}
