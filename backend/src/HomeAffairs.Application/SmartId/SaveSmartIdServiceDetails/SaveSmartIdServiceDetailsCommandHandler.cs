using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.SmartId;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Domain.Common.Exceptions;
using HomeAffairs.Domain.SmartId;
using MediatR;

namespace HomeAffairs.Application.SmartId.SaveSmartIdServiceDetails;

internal sealed class SaveSmartIdServiceDetailsCommandHandler(
    ISmartIdServiceRepository repository,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<
        SaveSmartIdServiceDetailsCommand,
        SmartIdServiceDetailsResult>
{
    public async Task<SmartIdServiceDetailsResult> Handle(
        SaveSmartIdServiceDetailsCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        var details =
            new SmartIdServiceDetails
            {
                SmartIdServiceDetailsId =
                    Guid.NewGuid(),

                ApplicationId =
                    request.ApplicationId,

                ExistingSmartIdNumber =
                    Normalize(
                        request.ExistingSmartIdNumber),

                RequestedFirstName =
                    Normalize(
                        request.RequestedFirstName),

                RequestedMiddleNames =
                    Normalize(
                        request.RequestedMiddleNames),

                RequestedLastName =
                    Normalize(
                        request.RequestedLastName),

                RequestedDateOfBirth =
                    request.RequestedDateOfBirth,

                RequestedSex =
                    NormalizeUpper(
                        request.RequestedSex),

                ReplacementReason =
                    Normalize(
                        request.ReplacementReason),

                IncidentDate =
                    request.IncidentDate,

                PoliceCaseNumber =
                    NormalizeUpper(
                        request.PoliceCaseNumber),

                PoliceStation =
                    Normalize(
                        request.PoliceStation),

                DamageDescription =
                    Normalize(
                        request.DamageDescription),

                CorrectionField =
                    NormalizeUpper(
                        request.CorrectionField),

                CurrentValue =
                    Normalize(
                        request.CurrentValue),

                RequestedValue =
                    Normalize(
                        request.RequestedValue),

                RequestReason =
                    Normalize(
                        request.RequestReason),

                CreatedAtUtc =
                    clock.UtcNow
            };

        try
        {
            var saved =
                await repository
                    .SaveForApplicantAsync(
                        details,
                        currentUser.UserId.Value,
                        request.ExpectedRowVersion,
                        clock.UtcNow,
                        cancellationToken);

            return Map(saved);
        }
        catch (
            SmartIdApplicationNotFoundPersistenceException)
        {
            throw new
                SmartIdServiceApplicationNotFoundException();
        }
        catch (
            InvalidSmartIdApplicationTypePersistenceException)
        {
            throw new
                InvalidSmartIdServiceApplicationException();
        }
        catch (
            SmartIdDetailsNotEditablePersistenceException)
        {
            throw new
                SmartIdServiceDetailsNotEditableException();
        }
        catch (
            SmartIdDetailsConcurrencyPersistenceException)
        {
            throw new
                SmartIdServiceDetailsConflictException();
        }
    }

    internal static SmartIdServiceDetailsResult Map(
        SmartIdServiceDetails details)
    {
        return new SmartIdServiceDetailsResult(
            details.SmartIdServiceDetailsId,
            details.ApplicationId,
            details.ApplicationType,
            details.ExistingSmartIdNumber,
            details.RequestedFirstName,
            details.RequestedMiddleNames,
            details.RequestedLastName,
            details.RequestedDateOfBirth,
            details.RequestedSex,
            details.ReplacementReason,
            details.IncidentDate,
            details.PoliceCaseNumber,
            details.PoliceStation,
            details.DamageDescription,
            details.CorrectionField,
            details.CurrentValue,
            details.RequestedValue,
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
