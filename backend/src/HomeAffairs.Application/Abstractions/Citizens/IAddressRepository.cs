using HomeAffairs.Domain.Citizens;

namespace HomeAffairs.Application.Abstractions.Citizens;

public interface IAddressRepository
{
    Task<IReadOnlyCollection<CitizenAddress>>
        GetByCitizenIdAsync(
            Guid citizenId,
            CancellationToken cancellationToken);

    Task<CitizenAddress?> GetByIdForUserAsync(
        Guid addressId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<CitizenAddress> CreateAsync(
        CitizenAddress address,
        Guid userId,
        CancellationToken cancellationToken);

    Task<CitizenAddress> UpdateAsync(
        CitizenAddress address,
        Guid userId,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        Guid addressId,
        Guid userId,
        byte[] expectedRowVersion,
        DateTimeOffset deletedAtUtc,
        CancellationToken cancellationToken);
}
