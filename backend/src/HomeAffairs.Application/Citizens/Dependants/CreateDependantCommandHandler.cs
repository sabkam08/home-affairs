using HomeAffairs.Application.Abstractions.Citizens;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Domain.Common.Exceptions;
using HomeAffairs.Domain.Citizens;
using MediatR;

namespace HomeAffairs.Application.Citizens.Dependants;

internal sealed class CreateDependantCommandHandler(
    ICitizenRepository citizenRepository,
    ICitizenRelationshipRepository relationshipRepository,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<
        CreateDependantCommand,
        DependantResult>
{
    public async Task<DependantResult> Handle(
        CreateDependantCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        var representative =
            await citizenRepository.GetByUserIdAsync(
                currentUser.UserId.Value,
                cancellationToken);

        if (representative is null)
        {
            throw new CitizenProfileNotFoundException();
        }

        var today =
            DateOnly.FromDateTime(
                clock.UtcNow.UtcDateTime);

        if (representative.IsMinor(today))
        {
            throw new RepresentativeMustBeAdultException();
        }

        var subject =
            new Citizen
            {
                CitizenId = Guid.NewGuid(),
                UserId = null,

                SouthAfricanIdNumber =
                    Normalize(
                        request.SouthAfricanIdNumber),

                FirstName =
                    request.FirstName.Trim(),

                MiddleNames =
                    Normalize(
                        request.MiddleNames),

                LastName =
                    request.LastName.Trim(),

                DateOfBirth =
                    request.DateOfBirth,

                Sex =
                    request.Sex
                        .Trim()
                        .ToUpperInvariant(),

                IsActive = true,

                CreatedAtUtc =
                    clock.UtcNow
            };

        if (!subject.IsMinor(today))
        {
            throw new SubjectMustBeMinorException();
        }

        var relationship =
            await relationshipRepository
                .CreateRepresentedMinorAsync(
                    subject,
                    Guid.NewGuid(),
                    currentUser.UserId.Value,
                    request.RelationshipType
                        .Trim()
                        .ToUpperInvariant(),
                    cancellationToken);

        return Map(
            relationship,
            today);
    }

    internal static DependantResult Map(
        CitizenRelationship relationship,
        DateOnly today)
    {
        return new DependantResult(
            relationship.CitizenRelationshipId,
            relationship.RepresentativeCitizenId,
            relationship.SubjectCitizenId,
            relationship.RelationshipType,
            relationship.Subject.SouthAfricanIdNumber,
            relationship.Subject.FirstName,
            relationship.Subject.MiddleNames,
            relationship.Subject.LastName,
            relationship.Subject.DateOfBirth,
            relationship.Subject.Sex,
            relationship.Subject.IsMinor(today),
            relationship.Subject.IsActive,
            relationship.CreatedAtUtc,
            relationship.RowVersion,
            relationship.Subject.RowVersion);
    }

    private static string? Normalize(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
