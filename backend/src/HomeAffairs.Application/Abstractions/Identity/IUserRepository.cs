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

    Task<LoginAttemptResult> RecordFailedLoginAsync(
        Guid userId,
        int maximumFailedLoginAttempts,
        int lockoutDurationMinutes,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken);

    Task RecordSuccessfulLoginAsync(
        Guid userId,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken);

    Task CreateStaffAsync(
    User user,
    string roleCode,
    Guid createdByUserId,
    CancellationToken cancellationToken);

    Task AssignRoleAsync(
        Guid userId,
        string roleCode,
        Guid assignedByUserId,
        DateTimeOffset assignedAtUtc,
        CancellationToken cancellationToken);

    Task RemoveRoleAsync(
        Guid userId,
        string roleCode,
        CancellationToken cancellationToken);
}
