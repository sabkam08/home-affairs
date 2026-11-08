using HomeAffairs.Application.Abstractions.Applications;
using HomeAffairs.Application.Abstractions.Citizens;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Application.Applications.CreateApplication;
using HomeAffairs.Domain.Common.Exceptions;
using HomeAffairs.Application.Citizens;
using MediatR;

namespace HomeAffairs.Application.Applications.UpdateApplication;

internal sealed class UpdateApplicationCommandHandler(
    IApplicationRepository applicationRepository,
    ICitizenRepository citizenRepository,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<
        UpdateApplicationCommand,
        ApplicationResult>
{
    public async Task<ApplicationResult> Handle(
        UpdateApplicationCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        var applicant =
            await citizenRepository.GetByUserIdAsync(
                currentUser.UserId.Value,
                cancellationToken);

        if (applicant is null)
        {
            throw new CitizenProfileNotFoundException();
        }

        var subjectCitizenId =
            request.SubjectCitizenId ??
            applicant.CitizenId;

        try
        {
            var updated =
                await applicationRepository
                    .UpdateDraftAsync(
                        request.ApplicationId,
                        currentUser.UserId.Value,
                        request.ApplicationType
                            .Trim()
                            .ToUpperInvariant(),
                        subjectCitizenId,
                        request.ExpectedRowVersion,
                        clock.UtcNow,
                        cancellationToken);

            return CreateApplicationCommandHandler
                .Map(updated);
        }
        catch (ApplicationNotFoundPersistenceException)
        {
            throw new ApplicationNotFoundException();
        }
        catch (ApplicationConcurrencyException)
        {
            throw new ApplicationConflictException();
        }
        catch (InvalidApplicationStatePersistenceException)
        {
            throw new InvalidApplicationStateException(
                "Only draft applications may be updated.");
        }
        catch (
            ApplicationSubjectNotFoundPersistenceException)
        {
            throw new
                ApplicationSubjectNotFoundException();
        }
        catch (
            ApplicationSubjectAccessDeniedPersistenceException)
        {
            throw new
                ApplicationSubjectAccessDeniedException();
        }
    }
}
