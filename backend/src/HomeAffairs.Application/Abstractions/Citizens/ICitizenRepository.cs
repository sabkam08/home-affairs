using HomeAffairs.Domain.Citizens;

namespace HomeAffairs.Application.Abstractions.Citizens;

public interface ICitizenRepository
{
    Task<Citizen?> GetByIdAsync(
        Guid citizenId,
        CancellationToken cancellationToken);

    Task<Citizen?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<Citizen> CreateForUserAsync(
        Citizen citizen,
        CancellationToken cancellationToken);

    Task<Citizen> UpdateOwnProfileAsync(
        Citizen citizen,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken);
}
