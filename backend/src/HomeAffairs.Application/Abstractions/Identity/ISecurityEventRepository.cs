namespace HomeAffairs.Application.Abstractions.Identity;

public interface ISecurityEventRepository
{
    Task CreateAsync(
        SecurityEvent securityEvent,
        CancellationToken cancellationToken);
}
