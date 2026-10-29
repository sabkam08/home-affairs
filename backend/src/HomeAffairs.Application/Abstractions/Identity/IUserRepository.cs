using HomeAffairs.Domain.Identity;

namespace HomeAffairs.Application.Abstractions.Identity;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken);

    Task<User?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<string>> GetRolesAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task CreateCitizenAsync(
        User user,
        CancellationToken cancellationToken);
}
