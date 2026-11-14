using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HomeAffairs.Contracts.Applications;
using HomeAffairs.Contracts.Citizens;
using HomeAffairs.Contracts.Identity;
using HomeAffairs.IntegrationTests.Identity;
using HomeAffairs.IntegrationTests.Infrastructure;

namespace HomeAffairs.IntegrationTests.Applications;

[Collection(IntegrationTestCollection.Name)]
public sealed class ApplicationTests(
    HomeAffairsApiFactory factory)
{
    #region Integration Tests
    [Fact]
    public async Task Citizen_should_create_self_application()
    {
        var citizen =
            await CreateAdultCitizenAsync();

        var response =
            await CreateApplicationAsync(
                citizen.AccessToken,
                new CreateApplicationRequest(
                    "SMART_ID_REPLACEMENT_LOST",
                    null));

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var application =
            await response.Content
                .ReadFromJsonAsync<
                    ApplicationResponse>();

        application.Should().NotBeNull();

        application!.Status
            .Should()
            .Be("DRAFT");

        application.IsSelfApplication
            .Should()
            .BeTrue();

        application.ApplicantCitizenId
            .Should()
            .Be(application.SubjectCitizenId);
    }

    [Fact]
    public async Task Citizen_should_create_application_for_owned_dependant()
    {
        var citizen =
            await CreateAdultCitizenAsync();

        var dependant =
            await CreateDependantAsync(
                citizen.AccessToken);

        var response =
            await CreateApplicationAsync(
                citizen.AccessToken,
                new CreateApplicationRequest(
                    "BIRTH_CERTIFICATE_REISSUE",
                    dependant.SubjectCitizenId));

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var application =
            await response.Content
                .ReadFromJsonAsync<
                    ApplicationResponse>();

        application.Should().NotBeNull();

        application!.IsSelfApplication
            .Should()
            .BeFalse();

        application.SubjectCitizenId
            .Should()
            .Be(dependant.SubjectCitizenId);
    }

    [Fact]
    public async Task Citizen_should_not_create_application_for_unrelated_subject()
    {
        var firstAdult =
            await CreateAdultCitizenAsync();

        var secondAdult =
            await CreateAdultCitizenAsync();

        var secondDependant =
            await CreateDependantAsync(
                secondAdult.AccessToken);

        var response =
            await CreateApplicationAsync(
                firstAdult.AccessToken,
                new CreateApplicationRequest(
                    "BIRTH_CERTIFICATE_REISSUE",
                    secondDependant.SubjectCitizenId));

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Citizen_should_read_owned_application()
    {
        var citizen =
            await CreateAdultCitizenAsync();

        var createResponse =
            await CreateApplicationAsync(
                citizen.AccessToken,
                new CreateApplicationRequest(
                    "SMART_ID_CORRECTION",
                    null));

        var created =
            await createResponse.Content
                .ReadFromJsonAsync<
                    ApplicationResponse>();

        created.Should().NotBeNull();

        using var request =
            AuthenticatedRequest(
                HttpMethod.Get,
                $"/api/v1/applications/" +
                $"{created!.ApplicationId}",
                citizen.AccessToken);

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Other_citizen_should_not_read_application()
    {
        var owner =
            await CreateAdultCitizenAsync();

        var createResponse =
            await CreateApplicationAsync(
                owner.AccessToken,
                new CreateApplicationRequest(
                    "SMART_ID_CORRECTION",
                    null));

        var application =
            await createResponse.Content
                .ReadFromJsonAsync<
                    ApplicationResponse>();

        application.Should().NotBeNull();

        var other =
            await CreateAdultCitizenAsync();

        using var request =
            AuthenticatedRequest(
                HttpMethod.Get,
                $"/api/v1/applications/" +
                $"{application!.ApplicationId}",
                other.AccessToken);

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Mine_should_return_self_and_dependant_applications()
    {
        var citizen =
            await CreateAdultCitizenAsync();

        var dependant =
            await CreateDependantAsync(
                citizen.AccessToken);

        await CreateApplicationAsync(
            citizen.AccessToken,
            new CreateApplicationRequest(
                "SMART_ID_CORRECTION",
                null));

        await CreateApplicationAsync(
            citizen.AccessToken,
            new CreateApplicationRequest(
                "BIRTH_CERTIFICATE_REISSUE",
                dependant.SubjectCitizenId));

        using var request =
            AuthenticatedRequest(
                HttpMethod.Get,
                "/api/v1/applications/mine",
                citizen.AccessToken);

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var applications =
            await response.Content
                .ReadFromJsonAsync<
                    ApplicationResponse[]>();

        applications.Should().NotBeNull();

        applications!
            .Should()
            .HaveCount(2);

        applications
            .Should()
            .Contain(application =>
                application.IsSelfApplication);

        applications
            .Should()
            .Contain(application =>
                !application.IsSelfApplication);
    }

    [Fact]
    public async Task Draft_creation_should_write_status_history()
    {
        var citizen =
            await CreateAdultCitizenAsync();

        var response =
            await CreateApplicationAsync(
                citizen.AccessToken,
                new CreateApplicationRequest(
                    "SMART_ID_NEW",
                    null));

        var application =
            await response.Content
                .ReadFromJsonAsync<
                    ApplicationResponse>();

        application.Should().NotBeNull();

        var historyCount =
            await factory.QueryScalarAsync<int>(
                $"""
                 SELECT COUNT(1)
                 FROM [applications].ApplicationStatusHistory
                 WHERE ApplicationId =
                     '{application!.ApplicationId}'
                   AND FromStatus IS NULL
                   AND ToStatus = 'DRAFT';
                 """);

        historyCount
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task Unsupported_application_type_should_return_bad_request()
    {
        var citizen =
            await CreateAdultCitizenAsync();

        var response =
            await CreateApplicationAsync(
                citizen.AccessToken,
                new CreateApplicationRequest(
                    "UNKNOWN_SERVICE",
                    null));

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.BadRequest);
    }

    private async Task<TestAuthenticationResponse>
        CreateAdultCitizenAsync()
    {
        var identity =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/v1/citizens/me");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                identity.AccessToken);

        request.Content =
            JsonContent.Create(
                new CreateCitizenProfileRequest(
                    null,
                    "Application",
                    null,
                    "Applicant",
                    new DateOnly(
                        1990,
                        1,
                        1),
                    "F"));

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        return identity;
    }

    private async Task<DependantResponse>
        CreateDependantAsync(
            string accessToken)
    {
        var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/v1/citizens/me/dependants");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        request.Content =
            JsonContent.Create(
                new CreateDependantRequest(
                    null,
                    "Application",
                    null,
                    "Subject",
                    new DateOnly(
                        2015,
                        1,
                        1),
                    "F",
                    "BIOLOGICAL_PARENT"));

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var dependant =
            await response.Content
                .ReadFromJsonAsync<
                    DependantResponse>();

        dependant.Should().NotBeNull();

        return dependant!;
    }

    private async Task<HttpResponseMessage>
        CreateApplicationAsync(
            string accessToken,
            CreateApplicationRequest application)
    {
        var request =
            AuthenticatedRequest(
                HttpMethod.Post,
                "/api/v1/applications",
                accessToken);

        request.Content =
            JsonContent.Create(application);

        return await factory.Client.SendAsync(
            request);
    }

    private static HttpRequestMessage
        AuthenticatedRequest(
            HttpMethod method,
            string uri,
            string accessToken)
    {
        var request =
            new HttpRequestMessage(
                method,
                uri);

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        return request;
    }

    [Fact]
    public async Task Applicant_should_update_draft()
    {
        var citizen =
            await CreateAdultCitizenAsync();

        var createResponse =
            await CreateApplicationAsync(
                citizen.AccessToken,
                new CreateApplicationRequest(
                    "SMART_ID_CORRECTION",
                    null));

        var created =
            await createResponse.Content
                .ReadFromJsonAsync<ApplicationResponse>();

        created.Should().NotBeNull();

        var updateResponse =
            await UpdateApplicationAsync(
                citizen.AccessToken,
                created!.ApplicationId,
                new UpdateApplicationRequest(
                    "SMART_ID_REPLACEMENT_DAMAGED",
                    null,
                    created.RowVersion));

        updateResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var updated =
            await updateResponse.Content
                .ReadFromJsonAsync<ApplicationResponse>();

        updated.Should().NotBeNull();

        updated!.ApplicationType
            .Should()
            .Be("SMART_ID_REPLACEMENT_DAMAGED");

        updated.Status
            .Should()
            .Be("DRAFT");

        updated.RowVersion
            .Should()
            .NotBe(created.RowVersion);
    }

    [Fact]
    public async Task Stale_draft_update_should_return_conflict()
    {
        var citizen =
            await CreateAdultCitizenAsync();

        var createResponse =
            await CreateApplicationAsync(
                citizen.AccessToken,
                new CreateApplicationRequest(
                    "SMART_ID_CORRECTION",
                    null));

        var created =
            await createResponse.Content
                .ReadFromJsonAsync<ApplicationResponse>();

        created.Should().NotBeNull();

        var originalRowVersion =
            created!.RowVersion;

        var first =
            await UpdateApplicationAsync(
                citizen.AccessToken,
                created.ApplicationId,
                new UpdateApplicationRequest(
                    "SMART_ID_REPLACEMENT_DAMAGED",
                    null,
                    originalRowVersion));

        first.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var stale =
            await UpdateApplicationAsync(
                citizen.AccessToken,
                created.ApplicationId,
                new UpdateApplicationRequest(
                    "SMART_ID_REPLACEMENT_LOST",
                    null,
                    originalRowVersion));

        stale.StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Applicant_should_submit_draft()
    {
        var citizen =
            await CreateAdultCitizenAsync();

        var submitted =
            await CreateAndSubmitAsync(
                citizen);

        submitted.Status
            .Should()
            .Be("SUBMITTED");

        submitted.SubmittedAtUtc
            .Should()
            .NotBeNull();
    }

    [Fact]
    public async Task Submitted_application_should_not_be_editable_as_draft()
    {
        var citizen =
            await CreateAdultCitizenAsync();

        var submitted =
            await CreateAndSubmitAsync(
                citizen);

        var update =
            await UpdateApplicationAsync(
                citizen.AccessToken,
                submitted.ApplicationId,
                new UpdateApplicationRequest(
                    "BIRTH_CERTIFICATE_REISSUE",
                    null,
                    submitted.RowVersion));

        update.StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Applicant_should_cancel_draft()
    {
        var citizen =
            await CreateAdultCitizenAsync();

        var createResponse =
            await CreateApplicationAsync(
                citizen.AccessToken,
                new CreateApplicationRequest(
                    "SMART_ID_CORRECTION",
                    null));

        var created =
            await createResponse.Content
                .ReadFromJsonAsync<ApplicationResponse>();

        created.Should().NotBeNull();

        var response =
            await CancelApplicationAsync(
                citizen.AccessToken,
                created!.ApplicationId,
                created.RowVersion,
                "No longer required.");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var cancelled =
            await response.Content
                .ReadFromJsonAsync<ApplicationResponse>();

        cancelled.Should().NotBeNull();

        cancelled!.Status
            .Should()
            .Be("CANCELLED");
    }

    [Fact]
    public async Task Submission_should_write_status_history()
    {
        var citizen =
            await CreateAdultCitizenAsync();

        var submitted =
            await CreateAndSubmitAsync(
                citizen);

        using var historyRequest =
            AuthenticatedRequest(
                HttpMethod.Get,
                $"/api/v1/applications/" +
                $"{submitted.ApplicationId}/history",
                citizen.AccessToken);

        var response =
            await factory.Client.SendAsync(
                historyRequest);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var history =
            await response.Content
                .ReadFromJsonAsync<
                    ApplicationStatusHistoryResponse[]>();

        history.Should().NotBeNull();

        history!
            .Should()
            .HaveCount(2);

        history[0].FromStatus
            .Should()
            .BeNull();

        history[0].ToStatus
            .Should()
            .Be("DRAFT");

        history[1].FromStatus
            .Should()
            .Be("DRAFT");

        history[1].ToStatus
            .Should()
            .Be("SUBMITTED");
    }

    [Fact]
    public async Task Other_citizen_should_not_update_application()
    {
        var owner =
            await CreateAdultCitizenAsync();

        var createResponse =
            await CreateApplicationAsync(
                owner.AccessToken,
                new CreateApplicationRequest(
                    "SMART_ID_CORRECTION",
                    null));

        var application =
            await createResponse.Content
                .ReadFromJsonAsync<ApplicationResponse>();

        application.Should().NotBeNull();

        var other =
            await CreateAdultCitizenAsync();

        var response =
            await UpdateApplicationAsync(
                other.AccessToken,
                application!.ApplicationId,
                new UpdateApplicationRequest(
                    "SMART_ID_REPLACEMENT_LOST",
                    null,
                    application.RowVersion));

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);
    }

    private async Task<HttpResponseMessage>
    UpdateApplicationAsync(
        string accessToken,
        Guid applicationId,
        UpdateApplicationRequest update)
    {
        var request =
            AuthenticatedRequest(
                HttpMethod.Put,
                $"/api/v1/applications/" +
                $"{applicationId}",
                accessToken);

        request.Content =
            JsonContent.Create(update);

        return await factory.Client.SendAsync(
            request);
    }

    private async Task<HttpResponseMessage>
        SubmitApplicationAsync(
            string accessToken,
            Guid applicationId,
            string rowVersion)
    {
        var request =
            AuthenticatedRequest(
                HttpMethod.Post,
                $"/api/v1/applications/" +
                $"{applicationId}/submit",
                accessToken);

        request.Content =
            JsonContent.Create(
                new ApplicationActionRequest(
                    rowVersion));

        return await factory.Client.SendAsync(
            request);
    }

    private async Task<HttpResponseMessage>
        CancelApplicationAsync(
            string accessToken,
            Guid applicationId,
            string rowVersion,
            string? reason)
    {
        var request =
            AuthenticatedRequest(
                HttpMethod.Post,
                $"/api/v1/applications/" +
                $"{applicationId}/cancel",
                accessToken);

        request.Content =
            JsonContent.Create(
                new CancelApplicationRequest(
                    rowVersion,
                    reason));

        return await factory.Client.SendAsync(
            request);
    }

    [Fact]
    public async Task Assignment_should_reject_non_practitioner()
    {
        var citizen =
            await CreateAdultCitizenAsync();

        var application =
            await CreateAndSubmitAsync(
                citizen);

        var otherCitizen =
            await CreateAdultCitizenAsync();

        var response =
            await AssignAsync(
                citizen.AccessToken,
                application.ApplicationId,
                otherCitizen.UserId,
                application.RowVersion);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Application_should_be_assigned_and_reassigned()
    {
        var citizen =
            await CreateAdultCitizenAsync();

        var submitted =
            await CreateAndSubmitAsync(
                citizen);

        var practitionerOne =
            await CreateStaffAsync(
                "PRACTITIONER");

        var practitionerTwo =
            await CreateStaffAsync(
                "PRACTITIONER");

        var firstAssignment =
            await AssignAsync(
                practitionerOne.AccessToken,
                submitted.ApplicationId,
                practitionerOne.UserId,
                submitted.RowVersion);

        firstAssignment.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var afterFirst =
            await GetApplicationAsync(
                citizen.AccessToken,
                submitted.ApplicationId);

        afterFirst.Status
            .Should()
            .Be("IN_PROGRESS");

        var secondAssignment =
            await AssignAsync(
                practitionerOne.AccessToken,
                submitted.ApplicationId,
                practitionerTwo.UserId,
                afterFirst.RowVersion);

        secondAssignment.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var history =
            await GetAssignmentHistoryAsync(
                practitionerOne.AccessToken,
                submitted.ApplicationId);

        history.Should().HaveCount(2);

        history[0].IsActive.Should().BeFalse();
        history[0].UnassignedAtUtc.Should().NotBeNull();

        history[1].IsActive.Should().BeTrue();

        history[1].AssignedToUserId
            .Should()
            .Be(practitionerTwo.UserId);
    }

    [Fact]
    public async Task Assigned_application_should_move_between_practitioner_queues()
    {
        var citizen =
            await CreateAdultCitizenAsync();

        var application =
            await CreateAndSubmitAsync(citizen);

        var first =
            await CreateStaffAsync("PRACTITIONER");

        var second =
            await CreateStaffAsync("PRACTITIONER");

        await AssignAsync(
            first.AccessToken,
            application.ApplicationId,
            first.UserId,
            application.RowVersion);

        var updated =
            await GetApplicationAsync(
                citizen.AccessToken,
                application.ApplicationId);

        var firstQueue =
            await GetAssignedAsync(
                first.AccessToken);

        firstQueue
            .Should()
            .Contain(x =>
                x.ApplicationId ==
                application.ApplicationId);

        await AssignAsync(
            first.AccessToken,
            application.ApplicationId,
            second.UserId,
            updated.RowVersion);

        firstQueue =
            await GetAssignedAsync(
                first.AccessToken);

        var secondQueue =
            await GetAssignedAsync(
                second.AccessToken);

        firstQueue
            .Should()
            .NotContain(x =>
                x.ApplicationId ==
                application.ApplicationId);

        secondQueue
            .Should()
            .Contain(x =>
                x.ApplicationId ==
                application.ApplicationId);
    }

    [Fact]
    public async Task Assigned_practitioner_should_request_information()
    {
        var setup =
            await CreateAssignedApplicationAsync();

        var response =
            await ProcessAsync(
                setup.Practitioner.AccessToken,
                setup.Application.ApplicationId,
                "request-information",
                setup.Application.RowVersion,
                "Additional evidence required.");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var updated =
            await response.Content
                .ReadFromJsonAsync<ApplicationResponse>();

        updated.Should().NotBeNull();

        updated!.Status
            .Should()
            .Be("AWAITING_INFORMATION");

        updated.RowVersion
            .Should()
            .NotBe(setup.Application.RowVersion);
    }

    [Fact]
    public async Task Assigned_practitioner_should_resume_after_information_received()
    {
        var setup =
            await CreateAssignedApplicationAsync();

        var requestInfo =
            await ProcessAsync(
                setup.Practitioner.AccessToken,
                setup.Application.ApplicationId,
                "request-information",
                setup.Application.RowVersion,
                "Additional evidence required.");

        var awaiting =
            await requestInfo.Content
                .ReadFromJsonAsync<ApplicationResponse>();

        awaiting.Should().NotBeNull();

        var resume =
            await ProcessAsync(
                setup.Practitioner.AccessToken,
                awaiting!.ApplicationId,
                "resume-processing",
                awaiting.RowVersion,
                "Evidence received.");

        resume.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var resumed =
            await resume.Content
                .ReadFromJsonAsync<ApplicationResponse>();

        resumed.Should().NotBeNull();

        resumed!.Status
            .Should()
            .Be("IN_PROGRESS");
    }

    [Fact]
    public async Task Assigned_practitioner_should_send_for_approval()
    {
        var setup =
            await CreateAssignedApplicationAsync();

        var response =
            await ProcessAsync(
                setup.Practitioner.AccessToken,
                setup.Application.ApplicationId,
                "send-for-approval",
                setup.Application.RowVersion,
                "Practitioner review completed.");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var updated =
            await response.Content
                .ReadFromJsonAsync<ApplicationResponse>();

        updated.Should().NotBeNull();

        updated!.Status
            .Should()
            .Be("AWAITING_APPROVAL");
    }

    [Fact]
    public async Task Illegal_practitioner_transition_should_return_conflict()
    {
        var setup =
            await CreateAssignedApplicationAsync();

        var approval =
            await ProcessAsync(
                setup.Practitioner.AccessToken,
                setup.Application.ApplicationId,
                "send-for-approval",
                setup.Application.RowVersion,
                "Review complete.");

        var awaitingApproval =
            await approval.Content
                .ReadFromJsonAsync<ApplicationResponse>();

        awaitingApproval.Should().NotBeNull();

        var illegal =
            await ProcessAsync(
                setup.Practitioner.AccessToken,
                awaitingApproval!.ApplicationId,
                "request-information",
                awaitingApproval.RowVersion,
                "Invalid transition.");

        illegal.StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Reassignment_should_transfer_processing_authority()
    {
        var citizen =
            await CreateAdultCitizenAsync();

        var submitted =
            await CreateAndSubmitAsync(citizen);

        var first =
            await CreateStaffAsync("PRACTITIONER");

        var second =
            await CreateStaffAsync("PRACTITIONER");

        await AssignAsync(
            first.AccessToken,
            submitted.ApplicationId,
            first.UserId,
            submitted.RowVersion);

        var afterFirst =
            await GetApplicationAsync(
                citizen.AccessToken,
                submitted.ApplicationId);

        await AssignAsync(
            first.AccessToken,
            submitted.ApplicationId,
            second.UserId,
            afterFirst.RowVersion);

        using var firstRequest =
            AuthenticatedRequest(
                HttpMethod.Get,
                $"/api/v1/applications/" +
                $"{submitted.ApplicationId}/assigned",
                first.AccessToken);

        var firstResponse =
            await factory.Client.SendAsync(firstRequest);

        firstResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);

        using var secondRequest =
            AuthenticatedRequest(
                HttpMethod.Get,
                $"/api/v1/applications/" +
                $"{submitted.ApplicationId}/assigned",
                second.AccessToken);

        var secondResponse =
            await factory.Client.SendAsync(secondRequest);

        secondResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Request_information_without_reason_should_return_bad_request()
    {
        var setup =
            await CreateAssignedApplicationAsync();

        var response =
            await ProcessAsync(
                setup.Practitioner.AccessToken,
                setup.Application.ApplicationId,
                "request-information",
                setup.Application.RowVersion,
                null);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Supervisor_should_approve_application()
    {
        var setup =
            await CreateAwaitingApprovalApplicationAsync();

        var response =
            await DecideAsync(
                setup.Supervisor.AccessToken,
                setup.Application.ApplicationId,
                "approve",
                setup.Application.RowVersion,
                "Approved by integration supervisor.");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var approved =
            await response.Content
                .ReadFromJsonAsync<ApplicationResponse>();

        approved.Should().NotBeNull();

        approved!.Status
            .Should()
            .Be("APPROVED");

        approved.RowVersion
            .Should()
            .NotBe(setup.Application.RowVersion);
    }

    [Fact]
    public async Task Supervisor_should_reject_application_with_reason()
    {
        var setup =
            await CreateAwaitingApprovalApplicationAsync();

        var response =
            await DecideAsync(
                setup.Supervisor.AccessToken,
                setup.Application.ApplicationId,
                "reject",
                setup.Application.RowVersion,
                "Supporting evidence is insufficient.");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var rejected =
            await response.Content
                .ReadFromJsonAsync<ApplicationResponse>();

        rejected.Should().NotBeNull();

        rejected!.Status
            .Should()
            .Be("REJECTED");
    }

    [Fact]
    public async Task Rejection_without_reason_should_return_bad_request()
    {
        var setup =
            await CreateAwaitingApprovalApplicationAsync();

        var response =
            await DecideAsync(
                setup.Supervisor.AccessToken,
                setup.Application.ApplicationId,
                "reject",
                setup.Application.RowVersion,
                null);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Practitioner_should_not_approve_application()
    {
        var setup =
            await CreateAwaitingApprovalApplicationAsync();

        var response =
            await DecideAsync(
                setup.Practitioner.AccessToken,
                setup.Application.ApplicationId,
                "approve",
                setup.Application.RowVersion,
                "Should not be accepted.");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Stale_supervisor_decision_should_return_conflict()
    {
        var setup =
            await CreateAwaitingApprovalApplicationAsync();

        var oldRowVersion =
            setup.Application.RowVersion;

        var first =
            await DecideAsync(
                setup.Supervisor.AccessToken,
                setup.Application.ApplicationId,
                "approve",
                oldRowVersion,
                "Approved.");

        first.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var stale =
            await DecideAsync(
                setup.Supervisor.AccessToken,
                setup.Application.ApplicationId,
                "reject",
                oldRowVersion,
                "Stale rejection.");

        stale.StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Supervisor_decision_should_close_active_assignment()
    {
        var setup =
            await CreateAwaitingApprovalApplicationAsync();

        var response =
            await DecideAsync(
                setup.Supervisor.AccessToken,
                setup.Application.ApplicationId,
                "approve",
                setup.Application.RowVersion,
                "Approved.");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var activeAssignmentCount =
            await factory.QueryScalarAsync<int>(
                $"""
             SELECT COUNT(1)
             FROM [applications].ApplicationAssignments
             WHERE ApplicationId =
                 '{setup.Application.ApplicationId}'
               AND IsActive = 1;
             """);

        activeAssignmentCount
            .Should()
            .Be(0);

        var closedBySupervisorCount =
            await factory.QueryScalarAsync<int>(
                $"""
             SELECT COUNT(1)
             FROM [applications].ApplicationAssignments
             WHERE ApplicationId =
                 '{setup.Application.ApplicationId}'
               AND IsActive = 0
               AND UnassignedByUserId =
                   '{setup.Supervisor.UserId}'
               AND UnassignedAtUtc IS NOT NULL;
             """);

        closedBySupervisorCount
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task Supervisor_decision_should_write_status_history()
    {
        var setup =
            await CreateAwaitingApprovalApplicationAsync();

        await DecideAsync(
            setup.Supervisor.AccessToken,
            setup.Application.ApplicationId,
            "reject",
            setup.Application.RowVersion,
            "Evidence rejected.");

        var count =
            await factory.QueryScalarAsync<int>(
                $"""
             SELECT COUNT(1)
             FROM [applications].ApplicationStatusHistory
             WHERE ApplicationId =
                 '{setup.Application.ApplicationId}'
               AND FromStatus =
                   'AWAITING_APPROVAL'
               AND ToStatus =
                   'REJECTED'
               AND ChangedByUserId =
                   '{setup.Supervisor.UserId}'
               AND Reason =
                   'Evidence rejected.';
             """);

        count.Should().Be(1);
    }

    [Fact]
    public async Task Approval_queue_should_show_only_awaiting_approval()
    {
        var setup =
            await CreateAwaitingApprovalApplicationAsync();

        using var request =
            AuthenticatedRequest(
                HttpMethod.Get,
                "/api/v1/applications/awaiting-approval",
                setup.Supervisor.AccessToken);

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var applications =
            await response.Content
                .ReadFromJsonAsync<ApplicationResponse[]>();

        applications.Should().NotBeNull();

        applications!
            .Should()
            .Contain(application =>
                application.ApplicationId ==
                setup.Application.ApplicationId);

        applications
            .Should()
            .OnlyContain(application =>
                application.Status ==
                "AWAITING_APPROVAL");
    }

    [Fact]
    public async Task Staff_should_search_applications()
    {
        var citizen =
            await CreateAdultCitizenAsync();

        var createResponse =
            await CreateApplicationAsync(
                citizen.AccessToken,
                new CreateApplicationRequest(
                    "SMART_ID_CORRECTION",
                    null));

        var created =
            await createResponse.Content
                .ReadFromJsonAsync<
                    ApplicationResponse>();

        created.Should().NotBeNull();

        var supervisor =
            await CreateStaffAsync(
                "SUPERVISOR");

        var result =
            await SearchAsync(
                supervisor.AccessToken,
                "pageNumber=1&pageSize=20");

        result.Items
            .Should()
            .Contain(application =>
                application.ApplicationId ==
                created!.ApplicationId);

        result.TotalCount
            .Should()
            .BeGreaterThan(0);
    }

    [Fact]
    public async Task Search_should_filter_by_status()
    {
        var citizen =
            await CreateAdultCitizenAsync();

        await CreateApplicationAsync(
            citizen.AccessToken,
            new CreateApplicationRequest(
                "SMART_ID_CORRECTION",
                null));

        var supervisor =
            await CreateStaffAsync(
                "SUPERVISOR");

        var result =
            await SearchAsync(
                supervisor.AccessToken,
                "status=DRAFT&pageNumber=1&pageSize=100");

        result.Items
            .Should()
            .NotBeEmpty();

        result.Items
            .Should()
            .OnlyContain(application =>
                application.Status == "DRAFT");
    }

    [Fact]
    public async Task Search_should_filter_by_application_type()
    {
        var citizen =
            await CreateAdultCitizenAsync();

        var createResponse =
            await CreateApplicationAsync(
                citizen.AccessToken,
                new CreateApplicationRequest(
                    "SMART_ID_REPLACEMENT_LOST",
                    null));

        var created =
            await createResponse.Content
                .ReadFromJsonAsync<
                    ApplicationResponse>();

        created.Should().NotBeNull();

        var supervisor =
            await CreateStaffAsync(
                "SUPERVISOR");

        var result =
            await SearchAsync(
                supervisor.AccessToken,
                "applicationType=" +
                "SMART_ID_REPLACEMENT_LOST" +
                "&pageNumber=1&pageSize=100");

        result.Items
            .Should()
            .Contain(application =>
                application.ApplicationId ==
                created!.ApplicationId);

        result.Items
            .Should()
            .OnlyContain(application =>
                application.ApplicationType ==
                "SMART_ID_REPLACEMENT_LOST");
    }

    [Fact]
    public async Task Search_should_filter_by_applicant()
    {
        var citizen =
            await CreateAdultCitizenAsync();

        var createResponse =
            await CreateApplicationAsync(
                citizen.AccessToken,
                new CreateApplicationRequest(
                    "SMART_ID_CORRECTION",
                    null));

        var created =
            await createResponse.Content
                .ReadFromJsonAsync<
                    ApplicationResponse>();

        created.Should().NotBeNull();

        var supervisor =
            await CreateStaffAsync(
                "SUPERVISOR");

        var result =
            await SearchAsync(
                supervisor.AccessToken,
                $"applicantCitizenId=" +
                $"{created!.ApplicantCitizenId}" +
                "&pageNumber=1&pageSize=100");

        result.Items
            .Should()
            .NotBeEmpty();

        result.Items
            .Should()
            .OnlyContain(application =>
                application.ApplicantCitizenId ==
                created.ApplicantCitizenId);
    }

    [Fact]
    public async Task Search_should_apply_database_paging()
    {
        /*
            Create five Applications using different Citizens.

            This deliberately respects the Smart ID invariant:
            one active Smart ID Application per SubjectCitizenId.

            The test is about paging, not duplicate Smart ID
            business-rule validation.
        */
        for (var index = 0;
             index < 5;
             index++)
        {
            var citizen =
                await CreateAdultCitizenAsync();

            var response =
                await CreateApplicationAsync(
                    citizen.AccessToken,
                    new CreateApplicationRequest(
                        "SMART_ID_CORRECTION",
                        null));

            response.StatusCode
                .Should()
                .Be(HttpStatusCode.Created);
        }

        var supervisor =
            await CreateStaffAsync(
                "SUPERVISOR");

        var pageOne =
            await SearchAsync(
                supervisor.AccessToken,
                "applicationType=" +
                "SMART_ID_CORRECTION" +
                "&pageNumber=1&pageSize=2");

        var pageTwo =
            await SearchAsync(
                supervisor.AccessToken,
                "applicationType=" +
                "SMART_ID_CORRECTION" +
                "&pageNumber=2&pageSize=2");

        pageOne.PageNumber
            .Should()
            .Be(1);

        pageTwo.PageNumber
            .Should()
            .Be(2);

        pageOne.PageSize
            .Should()
            .Be(2);

        pageTwo.PageSize
            .Should()
            .Be(2);

        pageOne.Items
            .Should()
            .HaveCount(2);

        pageTwo.Items
            .Should()
            .HaveCount(2);

        pageOne.Items
            .Should()
            .OnlyContain(application =>
                application.ApplicationType ==
                "SMART_ID_CORRECTION");

        pageTwo.Items
            .Should()
            .OnlyContain(application =>
                application.ApplicationType ==
                "SMART_ID_CORRECTION");

        pageOne.Items
            .Select(application =>
                application.ApplicationId)
            .Should()
            .NotIntersectWith(
                pageTwo.Items.Select(application =>
                    application.ApplicationId));

        pageOne.TotalCount
            .Should()
            .Be(pageTwo.TotalCount);

        pageOne.TotalCount
            .Should()
            .BeGreaterThanOrEqualTo(5);

        pageOne.TotalPages
            .Should()
            .BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task Search_page_size_over_limit_should_return_bad_request()
    {
        var supervisor =
            await CreateStaffAsync(
                "SUPERVISOR");

        using var request =
            AuthenticatedRequest(
                HttpMethod.Get,
                "/api/v1/applications/search" +
                "?pageNumber=1&pageSize=101",
                supervisor.AccessToken);

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Citizen_should_not_search_all_applications()
    {
        var citizen =
            await CreateAdultCitizenAsync();

        using var request =
            AuthenticatedRequest(
                HttpMethod.Get,
                "/api/v1/applications/search" +
                "?pageNumber=1&pageSize=20",
                citizen.AccessToken);

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Search_with_no_matches_should_return_empty_page()
    {
        var supervisor =
            await CreateStaffAsync(
                "SUPERVISOR");

        var result =
            await SearchAsync(
                supervisor.AccessToken,
                "applicationNumber=" +
                "DOES-NOT-EXIST-999999" +
                "&pageNumber=1&pageSize=20");

        result.Items
            .Should()
            .BeEmpty();

        result.TotalCount
            .Should()
            .Be(0);

        result.TotalPages
            .Should()
            .Be(0);
    }
    #endregion

    #region Helper Methods
    private async Task<TestAuthenticationResponse>
    CreateStaffAsync(
        string role)
    {
        var administrator =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        await IdentityTestHelper.AssignRoleDirectlyAsync(
            factory,
            administrator.UserId,
            "SECURITY_ADMINISTRATOR");

        var adminLogin =
            await IdentityTestHelper.LoginAsync(
                factory,
                administrator.Email);

        var staffEmail =
            IdentityTestHelper.UniqueEmail(
                role.ToLowerInvariant());

        var request =
            AuthenticatedRequest(
                HttpMethod.Post,
                "/api/v1/identity/users/staff",
                adminLogin.AccessToken);

        request.Content =
            JsonContent.Create(
                new CreateStaffUserRequest(
                    staffEmail,
                    "HomeAffairs-Test-2026!",
                    "Application",
                    "Staff",
                    role));

        var response =
            await factory.Client.SendAsync(
                request);

        response.EnsureSuccessStatusCode();

        return await IdentityTestHelper.LoginAsync(
            factory,
            staffEmail);
    }

    private async Task<ApplicationResponse>
    CreateAndSubmitAsync(
        TestAuthenticationResponse citizen)
    {
        /*
            Generic Application lifecycle tests should not depend
            on Smart ID service-specific submission validation.

            BIRTH_CERTIFICATE_REISSUE is used here as the neutral
            lifecycle fixture.

            We supply its required service details and document so
            the Application is legitimately ready for submission.
        */

        var create =
            await CreateApplicationAsync(
                citizen.AccessToken,
                new CreateApplicationRequest(
                    "BIRTH_CERTIFICATE_REISSUE",
                    null));

        create.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var draft =
            await create.Content
                .ReadFromJsonAsync<ApplicationResponse>();

        draft.Should().NotBeNull();

        /*
            Save the minimum valid Birth Re-Issue details.
        */

        using var birthRequest =
            AuthenticatedRequest(
                HttpMethod.Put,
                $"/api/v1/applications/" +
                $"{draft!.ApplicationId}/birth",
                citizen.AccessToken);

        birthRequest.Content =
            JsonContent.Create(
                new HomeAffairs.Contracts.Birth
                    .SaveBirthServiceDetailsRequest(
                        ChildFirstName: null,
                        ChildMiddleNames: null,
                        ChildLastName: null,
                        ChildDateOfBirth: null,
                        ChildSex: null,
                        PlaceOfBirth: null,
                        ProvinceOfBirth: null,
                        CountryOfBirth: null,
                        MotherFirstName: null,
                        MotherLastName: null,
                        MotherIdNumber: null,
                        FatherFirstName: null,
                        FatherLastName: null,
                        FatherIdNumber: null,
                        CurrentCertificateNumber:
                            "BC-INTEGRATION-TEST",
                        RequestReason:
                            "Generic application lifecycle integration test.",
                        RowVersion: null));

        var birthResponse =
            await factory.Client.SendAsync(
                birthRequest);

        birthResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        /*
            BIRTH_CERTIFICATE_REISSUE requires one
            IDENTITY_DOCUMENT.
        */

        await UploadApplicationTestDocumentAsync(
            citizen.AccessToken,
            draft.ApplicationId,
            "IDENTITY_DOCUMENT");

        /*
            The Birth-details/document writes do not mutate the
            Application row, so the original Application RowVersion
            remains valid for submission.
        */

        var submit =
            await SubmitApplicationAsync(
                citizen.AccessToken,
                draft.ApplicationId,
                draft.RowVersion);

        submit.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var submitted =
            await submit.Content
                .ReadFromJsonAsync<ApplicationResponse>();

        submitted.Should().NotBeNull();

        submitted!.Status
            .Should()
            .Be("SUBMITTED");

        return submitted;
    }

    private async Task
    UploadApplicationTestDocumentAsync(
        string accessToken,
        Guid applicationId,
        string documentType)
    {
        var content =
            Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

        using var multipart =
            new MultipartFormDataContent();

        multipart.Add(
            new StringContent(
                documentType),
            "DocumentType");

        var file =
            new ByteArrayContent(
                content);

        file.Headers.ContentType =
            new MediaTypeHeaderValue(
                "image/png");

        multipart.Add(
            file,
            "File",
            $"{documentType.ToLowerInvariant()}.png");

        using var request =
            AuthenticatedRequest(
                HttpMethod.Post,
                $"/api/v1/applications/" +
                $"{applicationId}/documents",
                accessToken);

        request.Content =
            multipart;

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);
    }

    private async Task<HttpResponseMessage>
        AssignAsync(
            string accessToken,
            Guid applicationId,
            Guid assignedToUserId,
            string rowVersion)
    {
        var request =
            AuthenticatedRequest(
                HttpMethod.Put,
                $"/api/v1/applications/" +
                $"{applicationId}/assignment",
                accessToken);

        request.Content =
            JsonContent.Create(
                new AssignApplicationRequest(
                    assignedToUserId,
                    rowVersion,
                    "Integration test assignment."));

        return await factory.Client.SendAsync(
            request);
    }

    private async Task<ApplicationResponse>
        GetApplicationAsync(
            string accessToken,
            Guid applicationId)
    {
        using var request =
            AuthenticatedRequest(
                HttpMethod.Get,
                $"/api/v1/applications/{applicationId}",
                accessToken);

        var response =
            await factory.Client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        return
            (await response.Content
                .ReadFromJsonAsync<ApplicationResponse>())!;
    }

    private async Task<ApplicationResponse[]>
        GetAssignedAsync(
            string accessToken)
    {
        using var request =
            AuthenticatedRequest(
                HttpMethod.Get,
                "/api/v1/applications/assigned-to-me",
                accessToken);

        var response =
            await factory.Client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        return
            (await response.Content
                .ReadFromJsonAsync<ApplicationResponse[]>())!;
    }

    private async Task<ApplicationAssignmentResponse[]>
        GetAssignmentHistoryAsync(
            string accessToken,
            Guid applicationId)
    {
        using var request =
            AuthenticatedRequest(
                HttpMethod.Get,
                $"/api/v1/applications/" +
                $"{applicationId}/assignments",
                accessToken);

        var response =
            await factory.Client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        return
            (await response.Content
                .ReadFromJsonAsync<
                    ApplicationAssignmentResponse[]>())!;
    }

    private sealed record AssignedApplicationSetup(
    TestAuthenticationResponse Citizen,
    TestAuthenticationResponse Practitioner,
    ApplicationResponse Application);

    private async Task<AssignedApplicationSetup>
    CreateAssignedApplicationAsync()
    {
        var citizen =
            await CreateAdultCitizenAsync();

        var submitted =
            await CreateAndSubmitAsync(
                citizen);

        var practitioner =
            await CreateStaffAsync(
                "PRACTITIONER");

        var assignmentResponse =
            await AssignAsync(
                practitioner.AccessToken,
                submitted.ApplicationId,
                practitioner.UserId,
                submitted.RowVersion);

        assignmentResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var inProgress =
            await GetApplicationAsync(
                citizen.AccessToken,
                submitted.ApplicationId);

        inProgress.Status
            .Should()
            .Be("IN_PROGRESS");

        return new AssignedApplicationSetup(
            citizen,
            practitioner,
            inProgress);
    }

    private async Task<HttpResponseMessage>
    ProcessAsync(
        string accessToken,
        Guid applicationId,
        string operation,
        string rowVersion,
        string? reason)
    {
        var request =
            AuthenticatedRequest(
                HttpMethod.Post,
                $"/api/v1/applications/" +
                $"{applicationId}/{operation}",
                accessToken);

        request.Content =
            JsonContent.Create(
                new ProcessApplicationRequest(
                    rowVersion,
                    reason));

        return await factory.Client.SendAsync(
            request);
    }

    private sealed record ApprovalApplicationSetup(
    TestAuthenticationResponse Citizen,
    TestAuthenticationResponse Practitioner,
    TestAuthenticationResponse Supervisor,
    ApplicationResponse Application);

    private async Task<ApprovalApplicationSetup>
    CreateAwaitingApprovalApplicationAsync()
    {
        var assigned =
            await CreateAssignedApplicationAsync();

        var supervisor =
            await CreateStaffAsync(
                "SUPERVISOR");

        var approvalResponse =
            await ProcessAsync(
                assigned.Practitioner.AccessToken,
                assigned.Application.ApplicationId,
                "send-for-approval",
                assigned.Application.RowVersion,
                "Practitioner review complete.");

        approvalResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var awaiting =
            await approvalResponse.Content
                .ReadFromJsonAsync<ApplicationResponse>();

        awaiting.Should().NotBeNull();

        awaiting!.Status
            .Should()
            .Be("AWAITING_APPROVAL");

        return new ApprovalApplicationSetup(
            assigned.Citizen,
            assigned.Practitioner,
            supervisor,
            awaiting);
    }

    private async Task<HttpResponseMessage>
    DecideAsync(
        string accessToken,
        Guid applicationId,
        string operation,
        string rowVersion,
        string? reason)
    {
        var request =
            AuthenticatedRequest(
                HttpMethod.Post,
                $"/api/v1/applications/" +
                $"{applicationId}/{operation}",
                accessToken);

        request.Content =
            JsonContent.Create(
                new ApplicationDecisionRequest(
                    rowVersion,
                    reason));

        return await factory.Client.SendAsync(
            request);
    }

    private async Task<ApplicationSearchResponse>
    SearchAsync(
        string accessToken,
        string queryString = "")
    {
        var uri =
            "/api/v1/applications/search";

        if (!string.IsNullOrWhiteSpace(
                queryString))
        {
            uri +=
                queryString.StartsWith(
                    "?",
                    StringComparison.Ordinal)
                    ? queryString
                    : $"?{queryString}";
        }

        using var request =
            AuthenticatedRequest(
                HttpMethod.Get,
                uri,
                accessToken);

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    ApplicationSearchResponse>();

        result.Should().NotBeNull();

        return result!;
    }
    #endregion
}
