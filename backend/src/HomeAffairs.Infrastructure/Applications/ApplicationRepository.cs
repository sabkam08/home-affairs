using System.Data;
using Dapper;
using HomeAffairs.Application.Abstractions.Applications;
using HomeAffairs.Application.Abstractions.Persistence;
using HomeAffairs.Application.Abstractions.Workflow;
using HomeAffairs.Domain.Applications;
using Microsoft.Data.SqlClient;

namespace HomeAffairs.Infrastructure.Applications;

internal sealed class ApplicationRepository(
    ISqlConnectionFactory connectionFactory)
    : IApplicationRepository
{
    public async Task<HomeAffairsApplication>
        CreateDraftAsync(
            HomeAffairsApplication application,
            Guid userId,
            CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "[applications].Application_CreateDraft",
            parameters: new
            {
                application.ApplicationId,
                application.ApplicationNumber,
                application.ApplicationType,
                application.ApplicantCitizenId,
                application.SubjectCitizenId,

                UserId =
                    userId,

                CreatedAtUtc =
                    application.CreatedAtUtc.UtcDateTime
            },
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        try
        {
            var record =
                await connection
                    .QuerySingleAsync<
                        ApplicationRecord>(
                            command);

            return record.ToDomain();
        }
        catch (SqlException exception)
            when (exception.Number == 52401)
        {
            throw new
                ApplicationApplicantNotFoundPersistenceException();
        }
        catch (SqlException exception)
            when (exception.Number == 52402)
        {
            throw new
                ApplicationSubjectNotFoundPersistenceException();
        }
        catch (SqlException exception)
            when (exception.Number == 52403)
        {
            throw new
                ApplicationSubjectAccessDeniedPersistenceException();
        }
    }

    public async Task<HomeAffairsApplication?>
        GetByIdForApplicantAsync(
            Guid applicationId,
            Guid userId,
            CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "[applications].Application_GetByIdForApplicant",
            parameters: new
            {
                ApplicationId =
                    applicationId,

                UserId =
                    userId
            },
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        var record =
            await connection
                .QuerySingleOrDefaultAsync<
                    ApplicationRecord>(
                        command);

        return record?.ToDomain();
    }

    public async Task<
        IReadOnlyCollection<HomeAffairsApplication>>
        GetMineAsync(
            Guid userId,
            CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "[applications].Application_GetMine",
            parameters: new
            {
                UserId =
                    userId
            },
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        var records =
            await connection.QueryAsync<
                ApplicationRecord>(
                    command);

        return records
            .Select(record => record.ToDomain())
            .ToArray();
    }

    public async Task<HomeAffairsApplication>
    UpdateDraftAsync(
        Guid applicationId,
        Guid userId,
        string applicationType,
        Guid subjectCitizenId,
        byte[] expectedRowVersion,
        DateTimeOffset updatedAtUtc,
        CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "[applications].Application_UpdateDraft",
            parameters: new
            {
                ApplicationId =
                    applicationId,

                UserId =
                    userId,

                ApplicationType =
                    applicationType,

                SubjectCitizenId =
                    subjectCitizenId,

                ExpectedRowVersion =
                    expectedRowVersion,

                UpdatedAtUtc =
                    updatedAtUtc.UtcDateTime
            },
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        try
        {
            var record =
                await connection.QuerySingleAsync<
                    ApplicationRecord>(command);

            return record.ToDomain();
        }
        catch (SqlException exception)
            when (exception.Number == 52410)
        {
            throw new
                ApplicationNotFoundPersistenceException();
        }
        catch (SqlException exception)
            when (exception.Number == 52411)
        {
            throw new
                InvalidApplicationStatePersistenceException(
                    "update");
        }
        catch (SqlException exception)
            when (exception.Number == 52412)
        {
            throw new
                ApplicationConcurrencyException();
        }
        catch (SqlException exception)
            when (exception.Number == 52402)
        {
            throw new
                ApplicationSubjectNotFoundPersistenceException();
        }
        catch (SqlException exception)
            when (exception.Number == 52403)
        {
            throw new
                ApplicationSubjectAccessDeniedPersistenceException();
        }
    }

    public async Task<HomeAffairsApplication>
    SubmitAsync(
        Guid applicationId,
        Guid userId,
        byte[] expectedRowVersion,
        DateTimeOffset submittedAtUtc,
        CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "[applications].Application_Submit",
            parameters: new
            {
                ApplicationId =
                    applicationId,

                UserId =
                    userId,

                ExpectedRowVersion =
                    expectedRowVersion,

                SubmittedAtUtc =
                    submittedAtUtc.UtcDateTime
            },
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        try
        {
            var record =
                await connection.QuerySingleAsync<
                    ApplicationRecord>(command);

            return record.ToDomain();
        }
        catch (SqlException exception)
            when (exception.Number == 52410)
        {
            throw new
                ApplicationNotFoundPersistenceException();
        }
        catch (SqlException exception)
            when (exception.Number == 52413)
        {
            throw new
                InvalidApplicationStatePersistenceException(
                    "submit");
        }
        catch (SqlException exception)
            when (exception.Number == 52412)
        {
            throw new
                ApplicationConcurrencyException();
        }
        catch (SqlException exception)
            when (exception.Number == 53601)
        {
            throw new
                ApplicationSubmissionNotReadyPersistenceException(
                    "Birth service details are required " +
                    "before submission.");
        }
        catch (SqlException exception)
            when (exception.Number == 53602)
        {
            throw new
                ApplicationSubmissionNotReadyPersistenceException(
                    exception.Message);
        }
        catch (SqlException exception)
            when (exception.Number == 53603)
        {
            throw new
                ApplicationSubmissionNotReadyPersistenceException(
                    "One or more required documents are missing.");
        }
    }

    public async Task<HomeAffairsApplication>
    CancelDraftAsync(
        Guid applicationId,
        Guid userId,
        byte[] expectedRowVersion,
        string? reason,
        DateTimeOffset cancelledAtUtc,
        CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "[applications].Application_CancelDraft",
            parameters: new
            {
                ApplicationId =
                    applicationId,

                UserId =
                    userId,

                ExpectedRowVersion =
                    expectedRowVersion,

                Reason =
                    reason,

                CancelledAtUtc =
                    cancelledAtUtc.UtcDateTime
            },
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        try
        {
            var record =
                await connection.QuerySingleAsync<
                    ApplicationRecord>(command);

            return record.ToDomain();
        }
        catch (SqlException exception)
            when (exception.Number == 52410)
        {
            throw new
                ApplicationNotFoundPersistenceException();
        }
        catch (SqlException exception)
            when (exception.Number == 52414)
        {
            throw new
                InvalidApplicationStatePersistenceException(
                    "cancel");
        }
        catch (SqlException exception)
            when (exception.Number == 52412)
        {
            throw new
                ApplicationConcurrencyException();
        }
    }

    public async Task<
    IReadOnlyCollection<ApplicationStatusHistory>>
    GetStatusHistoryForApplicantAsync(
        Guid applicationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "[applications].Application_GetStatusHistoryForApplicant",
            parameters: new
            {
                ApplicationId =
                    applicationId,

                UserId =
                    userId
            },
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        try
        {
            var records =
                await connection.QueryAsync<
                    ApplicationStatusHistoryRecord>(
                        command);

            return records
                .Select(record => record.ToDomain())
                .ToArray();
        }
        catch (SqlException exception)
            when (exception.Number == 52410)
        {
            throw new
                ApplicationNotFoundPersistenceException();
        }
    }

    public async Task<HomeAffairsApplication?>
    GetAssignedByIdAsync(
        Guid applicationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "[applications].Application_GetAssignedById",
            parameters: new
            {
                ApplicationId =
                    applicationId,

                UserId =
                    userId
            },
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        var record =
            await connection
                .QuerySingleOrDefaultAsync<
                    ApplicationRecord>(
                        command);

        return record?.ToDomain();
    }

    public async Task<HomeAffairsApplication>
    TransitionByAssignedPractitionerAsync(
        Guid applicationId,
        Guid userId,
        string targetStatus,
        string? reason,
        byte[] expectedRowVersion,
        DateTimeOffset changedAtUtc,
        CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "[applications].Application_TransitionByAssignedPractitioner",
            parameters: new
            {
                ApplicationId =
                    applicationId,

                UserId =
                    userId,

                TargetStatus =
                    targetStatus,

                Reason =
                    reason,

                ExpectedRowVersion =
                    expectedRowVersion,

                ChangedAtUtc =
                    changedAtUtc.UtcDateTime
            },
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        try
        {
            var record =
                await connection.QuerySingleAsync<
                    ApplicationRecord>(
                        command);

            return record.ToDomain();
        }
        catch (SqlException exception)
            when (exception.Number == 52601)
        {
            throw new
                ApplicationNotFoundPersistenceException();
        }
        catch (SqlException exception)
            when (exception.Number == 52602)
        {
            throw new
                InvalidApplicationStatePersistenceException(
                    "practitioner-transition");
        }
        catch (SqlException exception)
            when (exception.Number == 52603)
        {
            throw new
                ApplicationConcurrencyException();
        }
        catch (SqlException exception)
            when (exception.Number == 52906)
        {
            throw new WorkflowStateMismatchPersistenceException();
        }
    }

    public async Task<
    IReadOnlyCollection<HomeAffairsApplication>>
    GetAwaitingApprovalAsync(
        CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "[applications].Application_GetAwaitingApproval",
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        var records =
            await connection.QueryAsync<
                ApplicationRecord>(command);

        return records
            .Select(record => record.ToDomain())
            .ToArray();
    }

    public async Task<HomeAffairsApplication>
    DecideAsync(
        Guid applicationId,
        Guid decisionByUserId,
        string decision,
        string? reason,
        byte[] expectedRowVersion,
        DateTimeOffset decidedAtUtc,
        CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "[applications].Application_Decide",
            parameters: new
            {
                ApplicationId =
                    applicationId,

                DecisionByUserId =
                    decisionByUserId,

                Decision =
                    decision,

                Reason =
                    reason,

                ExpectedRowVersion =
                    expectedRowVersion,

                DecidedAtUtc =
                    decidedAtUtc.UtcDateTime
            },
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        try
        {
            var record =
                await connection.QuerySingleAsync<
                    ApplicationRecord>(command);

            return record.ToDomain();
        }
        catch (SqlException exception)
            when (exception.Number == 52701)
        {
            throw new
                ApplicationNotFoundPersistenceException();
        }
        catch (SqlException exception)
            when (exception.Number == 52702)
        {
            throw new
                InvalidApplicationStatePersistenceException(
                    "decision");
        }
        catch (SqlException exception)
            when (exception.Number is 52703 or 52704)
        {
            throw new
                InvalidApplicationDecisionException();
        }
        catch (SqlException exception)
            when (exception.Number == 52705)
        {
            throw new
                InvalidApplicationDecisionMakerException();
        }
        catch (SqlException exception)
            when (exception.Number == 52706)
        {
            throw new
                ApplicationAssignmentRequiredException();
        }
        catch (SqlException exception)
            when (exception.Number == 52707)
        {
            throw new
                SelfApprovalNotAllowedException();
        }
        catch (SqlException exception)
            when (exception.Number == 52708)
        {
            throw new
                ApplicationConcurrencyException();
        }
        catch (SqlException exception)
             when (exception.Number == 52906)
        {
            throw new WorkflowStateMismatchPersistenceException();
        }
    }

    public async Task<ApplicationSearchPage> SearchAsync(
    ApplicationSearchCriteria criteria,
    CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "[applications].Application_Search",
            parameters: new
            {
                criteria.ApplicationNumber,
                criteria.ApplicationType,
                criteria.Status,
                criteria.ApplicantCitizenId,
                criteria.SubjectCitizenId,
                criteria.AssignedToUserId,

                CreatedFromUtc =
                    criteria.CreatedFromUtc?
                        .UtcDateTime,

                CreatedToUtc =
                    criteria.CreatedToUtc?
                        .UtcDateTime,

                criteria.PageNumber,
                criteria.PageSize
            },
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        try
        {
            using var results =
                await connection.QueryMultipleAsync(
                    command);

            var totalCount =
                await results.ReadSingleAsync<int>();

            var records =
                (
                    await results.ReadAsync<
                        ApplicationRecord>()
                )
                .ToArray();

            var applications =
                records
                    .Select(record =>
                        record.ToDomain())
                    .ToArray();

            return new ApplicationSearchPage(
                applications,
                totalCount,
                criteria.PageNumber,
                criteria.PageSize);
        }
        catch (SqlException exception)
            when (exception.Number == 52801)
        {
            throw new InvalidApplicationSearchException(
                "Page number must be greater than zero.");
        }
        catch (SqlException exception)
            when (exception.Number == 52802)
        {
            throw new InvalidApplicationSearchException(
                "Page size must be between 1 and 100.");
        }
        catch (SqlException exception)
            when (exception.Number == 52803)
        {
            throw new InvalidApplicationSearchException(
                "Created-from date may not be later " +
                "than created-to date.");
        }
    }
}
