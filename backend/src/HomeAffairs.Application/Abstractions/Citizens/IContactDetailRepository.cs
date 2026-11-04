using HomeAffairs.Domain.Citizens;

namespace HomeAffairs.Application.Abstractions.Citizens;

public interface IContactDetailRepository
{
    Task<IReadOnlyCollection<ContactDetail>>
        GetByCitizenIdAsync(
            Guid citizenId,
            CancellationToken cancellationToken);

    Task<ContactDetail> CreateAsync(
        ContactDetail contactDetail,
        Guid userId,
        CancellationToken cancellationToken);
}
