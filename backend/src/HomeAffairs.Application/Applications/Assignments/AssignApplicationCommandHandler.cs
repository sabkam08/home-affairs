using HomeAffairs.Application.Applications;
using HomeAffairs.Application.Abstractions.Applications;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;

namespace HomeAffairs.Application.Applications.Assignments;

internal sealed class AssignApplicationCommandHandler(
    IApplicationAssignmentRepository repository,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<
        AssignApplicationCommand,
        ApplicationAssignmentResult>
{
    public async Task<ApplicationAssignmentResult> Handle(
        AssignApplicationCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        try
        {
            var assignment =
                await repository.AssignAsync(
                    request.ApplicationId,
                    request.AssignedToUserId,
                    currentUser.UserId.Value,
                    Normalize(request.Reason),
                    request.ExpectedApplicationRowVersion,
                    clock.UtcNow,
                    cancellationToken);

            return Map(assignment);
        }
        catch (ApplicationAssignmentNotFoundException)
        {
            throw new ApplicationNotFoundException();
        }
        catch (InvalidAssignmentStateException)
        {
            throw new InvalidApplicationStateException(
                "Only submitted or in-progress applications may be assigned.");
        }
        catch (InvalidAssigneeException)
        {
            throw new InvalidApplicationStateException(
                "The target user is not an active practitioner or supervisor.");
        }
        catch (ApplicationConcurrencyException)
        {
            throw new ApplicationConflictException();
        }
    }

    internal static ApplicationAssignmentResult Map(
        HomeAffairs.Domain.Applications.ApplicationAssignment x)
    {
        return new ApplicationAssignmentResult(
            x.ApplicationAssignmentId,
            x.ApplicationId,
            x.AssignedToUserId,
            x.AssignedByUserId,
            x.AssignedAtUtc,
            x.UnassignedAtUtc,
            x.UnassignedByUserId,
            x.Reason,
            x.IsActive,
            x.RowVersion);
    }

    private static string? Normalize(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
