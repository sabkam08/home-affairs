using HomeAffairs.Domain.SmartId;

namespace HomeAffairs.Application.Abstractions.SmartId;

public interface ISmartIdServiceRepository
{
    Task<SmartIdServiceDetails>
        SaveForApplicantAsync(
            SmartIdServiceDetails details,
            Guid userId,
            byte[]? expectedRowVersion,
            DateTimeOffset changedAtUtc,
            CancellationToken cancellationToken);

    Task<SmartIdServiceDetails?>
        GetForApplicantAsync(
            Guid applicationId,
            Guid userId,
            CancellationToken cancellationToken);
}
