using HomeAffairs.Application.Abstractions.Applications;
using HomeAffairs.Application.Abstractions.Citizens;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Application.Citizens;
using HomeAffairs.Domain.Common.Exceptions;
using HomeAffairs.Domain.Applications;
using MediatR;

namespace HomeAffairs.Application.Applications.CreateApplication;

internal sealed class CreateApplicationCommandHandler(
    IApplicationRepository applicationRepository,
    ICitizenRepository citizenRepository,
    IApplicationNumberGenerator applicationNumberGenerator,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<
        CreateApplicationCommand,
        ApplicationResult>
{
    public async Task<ApplicationResult> Handle(
        CreateApplicationCommand request,
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

        var createdAtUtc =
            clock.UtcNow;

        var application =
            new HomeAffairsApplication
            {
                ApplicationId =
                    Guid.NewGuid(),

                ApplicationNumber =
                    applicationNumberGenerator.Generate(
                        createdAtUtc),

                ApplicationType =
                    request.ApplicationType
                        .Trim()
                        .ToUpperInvariant(),

                ApplicantCitizenId =
                    applicant.CitizenId,

                SubjectCitizenId =
                    subjectCitizenId,

                Status =
                    ApplicationStatuses.Draft,

                SubmittedAtUtc =
                    null,

                CreatedAtUtc =
                    createdAtUtc
            };

        try
        {
            var created =
                await applicationRepository
                    .CreateDraftAsync(
                        application,
                        currentUser.UserId.Value,
                        cancellationToken);

            return Map(created);
        }
        catch (
            ApplicationSubjectNotFoundPersistenceException)
        {
            throw new ApplicationSubjectNotFoundException();
        }
        catch (
            ApplicationSubjectAccessDeniedPersistenceException)
        {
            throw new
                ApplicationSubjectAccessDeniedException();
        }
    }

    internal static ApplicationResult Map(
        HomeAffairsApplication application)
    {
        return new ApplicationResult(
            application.ApplicationId,
            application.ApplicationNumber,
            application.ApplicationType,
            application.ApplicantCitizenId,
            application.SubjectCitizenId,
            application.Status,
            application.IsSelfApplication,
            application.SubmittedAtUtc,
            application.CreatedAtUtc,
            application.UpdatedAtUtc,
            application.RowVersion);
    }
}
