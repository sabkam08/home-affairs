using HomeAffairs.Domain.Birth;

namespace HomeAffairs.Application.Abstractions.Birth;

public interface IBirthServiceRepository
{
    Task<BirthServiceDetails> SaveForApplicantAsync(
        BirthServiceDetails details,
        Guid userId,
        byte[]? expectedRowVersion,
        DateTimeOffset changedAtUtc,
        CancellationToken cancellationToken);

    Task<BirthServiceDetails?>
        GetForApplicantAsync(
            Guid applicationId,
            Guid userId,
            CancellationToken cancellationToken);
}
