using HomeAffairs.Domain.Citizens;

namespace HomeAffairs.Application.Abstractions.Citizens;

public interface IContactDetailRepository
{
    Task<IReadOnlyCollection<ContactDetail>>
        GetByCitizenIdAsync(
            Guid citizenId,
            CancellationToken cancellationToken);

    Task<ContactDetail?> GetByIdForUserAsync(
        Guid contactDetailId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<ContactDetail> CreateAsync(
        ContactDetail contactDetail,
        Guid userId,
        CancellationToken cancellationToken);

    Task<ContactDetail> UpdateAsync(
        ContactDetail contactDetail,
        Guid userId,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        Guid contactDetailId,
        Guid userId,
        byte[] expectedRowVersion,
        DateTimeOffset deletedAtUtc,
        CancellationToken cancellationToken);
}
