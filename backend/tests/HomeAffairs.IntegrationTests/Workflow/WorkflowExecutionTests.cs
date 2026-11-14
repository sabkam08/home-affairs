using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HomeAffairs.Contracts.Applications;
using HomeAffairs.Contracts.Citizens;
using HomeAffairs.Contracts.Identity;
using HomeAffairs.Contracts.Workflow;
using HomeAffairs.Contracts.Birth;
using HomeAffairs.IntegrationTests.Identity;
using HomeAffairs.IntegrationTests.Infrastructure;

namespace HomeAffairs.IntegrationTests.Workflow;

[Collection(IntegrationTestCollection.Name)]
public sealed class WorkflowExecutionTests(
    HomeAffairsApiFactory factory)
{
    [Fact]
    public async Task Full_approved_lifecycle_should_keep_application_and_workflow_synchronized()
    {
        var citizen =
            await CreateCitizenAsync();

        var practitioner =
            await CreateStaffAsync(
                "PRACTITIONER");

        var supervisor =
            await CreateStaffAsync(
                "SUPERVISOR");

        var application =
            await CreateApplicationAsync(
                citizen.AccessToken);

        await AssertSynchronizedAsync(
            citizen.AccessToken,
            application,
            "DRAFT",
            completed: false);

        application =
            await SubmitAsync(
                citizen.AccessToken,
                application);

        await AssertSynchronizedAsync(
            citizen.AccessToken,
            application,
            "SUBMITTED",
            completed: false);

        await AssignAsync(
            practitioner.AccessToken,
            practitioner.UserId,
            application);

        application =
            await GetApplicationAsync(
                citizen.AccessToken,
                application.ApplicationId);

        await AssertSynchronizedAsync(
            citizen.AccessToken,
            application,
            "IN_PROGRESS",
            completed: false);

        application =
            await ProcessAsync(
                practitioner.AccessToken,
                application,
                "request-information",
                "Additional evidence required.");

        await AssertSynchronizedAsync(
            citizen.AccessToken,
            application,
            "AWAITING_INFORMATION",
            completed: false);

        application =
            await ProcessAsync(
                practitioner.AccessToken,
                application,
                "resume-processing",
                "Evidence received.");

        await AssertSynchronizedAsync(
            citizen.AccessToken,
            application,
            "IN_PROGRESS",
            completed: false);

        application =
            await ProcessAsync(
                practitioner.AccessToken,
                application,
                "send-for-approval",
                "Review complete.");

        await AssertSynchronizedAsync(
            citizen.AccessToken,
            application,
            "AWAITING_APPROVAL",
            completed: false);

        application =
            await DecideAsync(
                supervisor.AccessToken,
                application,
                "approve",
                "Approved.");

        await AssertSynchronizedAsync(
            citizen.AccessToken,
            application,
            "APPROVED",
            completed: true);

        await AssertGlobalSynchronizationAsync();
    }

    [Fact]
    public async Task Rejected_lifecycle_should_complete_workflow_as_rejected()
    {
        var citizen =
            await CreateCitizenAsync();

        var practitioner =
            await CreateStaffAsync(
                "PRACTITIONER");

        var supervisor =
            await CreateStaffAsync(
                "SUPERVISOR");

        var application =
            await CreateApplicationAsync(
                citizen.AccessToken);

        application =
            await SubmitAsync(
                citizen.AccessToken,
                application);

        await AssignAsync(
            practitioner.AccessToken,
            practitioner.UserId,
            application);

        application =
            await GetApplicationAsync(
                citizen.AccessToken,
                application.ApplicationId);

        application =
            await ProcessAsync(
                practitioner.AccessToken,
                application,
                "send-for-approval",
                "Ready for supervisor.");

        application =
            await DecideAsync(
                supervisor.AccessToken,
                application,
                "reject",
                "Evidence insufficient.");

        await AssertSynchronizedAsync(
            citizen.AccessToken,
            application,
            "REJECTED",
            completed: true);

        await AssertGlobalSynchronizationAsync();
    }

    [Fact]
    public async Task Cancelled_draft_should_complete_workflow_as_cancelled()
    {
        var citizen =
            await CreateCitizenAsync();

        var application =
            await CreateApplicationAsync(
                citizen.AccessToken);

        var request =
            AuthenticatedRequest(
                HttpMethod.Post,
                $"/api/v1/applications/" +
                $"{application.ApplicationId}/cancel",
                citizen.AccessToken);

        request.Content =
            JsonContent.Create(
                new CancelApplicationRequest(
                    application.RowVersion,
                    "No longer required."));

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var cancelled =
            await response.Content
                .ReadFromJsonAsync<ApplicationResponse>();

        cancelled.Should().NotBeNull();

        await AssertSynchronizedAsync(
            citizen.AccessToken,
            cancelled!,
            "CANCELLED",
            completed: true);
    }

    [Fact]
    public async Task Reassignment_should_not_change_workflow_state()
    {
        var citizen =
            await CreateCitizenAsync();

        var first =
            await CreateStaffAsync(
                "PRACTITIONER");

        var second =
            await CreateStaffAsync(
                "PRACTITIONER");

        var application =
            await CreateApplicationAsync(
                citizen.AccessToken);

        application =
            await SubmitAsync(
                citizen.AccessToken,
                application);

        await AssignAsync(
            first.AccessToken,
            first.UserId,
            application);

        application =
            await GetApplicationAsync(
                citizen.AccessToken,
                application.ApplicationId);

        var before =
            await GetWorkflowAsync(
                citizen.AccessToken,
                application.ApplicationId);

        await AssignAsync(
            first.AccessToken,
            second.UserId,
            application);

        var afterApplication =
            await GetApplicationAsync(
                citizen.AccessToken,
                application.ApplicationId);

        var afterWorkflow =
            await GetWorkflowAsync(
                citizen.AccessToken,
                application.ApplicationId);

        afterApplication.Status
            .Should()
            .Be("IN_PROGRESS");

        afterWorkflow.CurrentStateCode
            .Should()
            .Be("IN_PROGRESS");

        afterWorkflow.WorkflowInstanceId
            .Should()
            .Be(before.WorkflowInstanceId);

        afterWorkflow.WorkflowDefinitionId
            .Should()
            .Be(before.WorkflowDefinitionId);

        await AssertGlobalSynchronizationAsync();
    }

    [Fact]
    public async Task Illegal_workflow_transition_should_not_create_state_mismatch()
    {
        var citizen =
            await CreateCitizenAsync();

        var practitioner =
            await CreateStaffAsync(
                "PRACTITIONER");

        var supervisor =
            await CreateStaffAsync(
                "SUPERVISOR");

        var application =
            await CreateApplicationAsync(
                citizen.AccessToken);

        application =
            await SubmitAsync(
                citizen.AccessToken,
                application);

        await AssignAsync(
            practitioner.AccessToken,
            practitioner.UserId,
            application);

        application =
            await GetApplicationAsync(
                citizen.AccessToken,
                application.ApplicationId);

        application =
            await ProcessAsync(
                practitioner.AccessToken,
                application,
                "send-for-approval",
                "Review complete.");

        application =
            await DecideAsync(
                supervisor.AccessToken,
                application,
                "approve",
                "Approved.");

        /*
            Application is terminal. A practitioner processing
            operation must fail and state synchronization must
            remain intact.
        */
        var request =
            AuthenticatedRequest(
                HttpMethod.Post,
                $"/api/v1/applications/" +
                $"{application.ApplicationId}/request-information",
                practitioner.AccessToken);

        request.Content =
            JsonContent.Create(
                new ProcessApplicationRequest(
                    application.RowVersion,
                    "Should fail."));

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .BeOneOf(
                HttpStatusCode.NotFound,
                HttpStatusCode.Conflict);

        await AssertGlobalSynchronizationAsync();
    }

    private async Task
        AssertSynchronizedAsync(
            string citizenAccessToken,
            ApplicationResponse application,
            string expectedState,
            bool completed)
    {
        application.Status
            .Should()
            .Be(expectedState);

        var workflow =
            await GetWorkflowAsync(
                citizenAccessToken,
                application.ApplicationId);

        workflow.CurrentStateCode
            .Should()
            .Be(expectedState);

        if (completed)
        {
            workflow.CompletedAtUtc
                .Should()
                .NotBeNull();
        }
        else
        {
            workflow.CompletedAtUtc
                .Should()
                .BeNull();
        }

        var mismatchCount =
            await factory.QueryScalarAsync<int>(
                $"""
                 SELECT COUNT(1)
                 FROM [applications].Applications AS a
                 INNER JOIN [workflow].WorkflowInstances AS wi
                     ON wi.ApplicationId =
                         a.ApplicationId
                 WHERE a.ApplicationId =
                         '{application.ApplicationId}'
                   AND a.Status <>
                         wi.CurrentStateCode;
                 """);

        mismatchCount
            .Should()
            .Be(0);
    }

    private async Task
        AssertGlobalSynchronizationAsync()
    {
        var count =
            await factory.QueryScalarAsync<int>(
                """
                SELECT COUNT(1)
                FROM [applications].Applications AS a
                INNER JOIN [workflow].WorkflowInstances AS wi
                    ON wi.ApplicationId =
                        a.ApplicationId
                WHERE a.Status <>
                    wi.CurrentStateCode;
                """);

        count.Should().Be(0);
    }

    private async Task<TestAuthenticationResponse>
        CreateCitizenAsync()
    {
        var identity =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        var request =
            AuthenticatedRequest(
                HttpMethod.Post,
                "/api/v1/citizens/me",
                identity.AccessToken);

        request.Content =
            JsonContent.Create(
                new CreateCitizenProfileRequest(
                    null,
                    "Workflow",
                    null,
                    "Execution",
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

    private async Task<
        TestAuthenticationResponse>
        CreateStaffAsync(
            string role)
    {
        var administrator =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        await IdentityTestHelper
            .AssignRoleDirectlyAsync(
                factory,
                administrator.UserId,
                "SECURITY_ADMINISTRATOR");

        var adminLogin =
            await IdentityTestHelper
                .LoginAsync(
                    factory,
                    administrator.Email);

        var email =
            IdentityTestHelper.UniqueEmail(
                $"workflow-{role.ToLowerInvariant()}");

        var request =
            AuthenticatedRequest(
                HttpMethod.Post,
                "/api/v1/identity/users/staff",
                adminLogin.AccessToken);

        request.Content =
            JsonContent.Create(
                new CreateStaffUserRequest(
                    email,
                    "HomeAffairs-Test-2026!",
                    "Workflow",
                    "Staff",
                    role));

        var response =
            await factory.Client.SendAsync(
                request);

        response.EnsureSuccessStatusCode();

        return await IdentityTestHelper
            .LoginAsync(
                factory,
                email);
    }

    private async Task<ApplicationResponse>
    CreateApplicationAsync(
        string accessToken)
    {
        /*
            Workflow lifecycle tests use a Birth Certificate
            Re-Issue application as their neutral valid fixture.

            Smart ID submission now has service-specific detail and
            document validation, so generic workflow tests should
            not depend on Smart ID correction rules.
        */

        using var request =
            AuthenticatedRequest(
                HttpMethod.Post,
                "/api/v1/applications",
                accessToken);

        request.Content =
            JsonContent.Create(
                new CreateApplicationRequest(
                    "BIRTH_CERTIFICATE_REISSUE",
                    null));

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var application =
            await response.Content
                .ReadFromJsonAsync<
                    ApplicationResponse>();

        application.Should().NotBeNull();

        return application!;
    }

    private async Task SaveBirthReissueDetailsAsync(
    string accessToken,
    Guid applicationId)
    {
        using var request =
            AuthenticatedRequest(
                HttpMethod.Put,
                $"/api/v1/applications/" +
                $"{applicationId}/birth",
                accessToken);

        request.Content =
            JsonContent.Create(
                new SaveBirthServiceDetailsRequest(
                    ChildFirstName:
                        null,

                    ChildMiddleNames:
                        null,

                    ChildLastName:
                        null,

                    ChildDateOfBirth:
                        null,

                    ChildSex:
                        null,

                    PlaceOfBirth:
                        null,

                    ProvinceOfBirth:
                        null,

                    CountryOfBirth:
                        null,

                    MotherFirstName:
                        null,

                    MotherLastName:
                        null,

                    MotherIdNumber:
                        null,

                    FatherFirstName:
                        null,

                    FatherLastName:
                        null,

                    FatherIdNumber:
                        null,

                    CurrentCertificateNumber:
                        "BC-WORKFLOW-TEST-001",

                    RequestReason:
                        "Workflow lifecycle integration test.",

                    RowVersion:
                        null));

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);
    }

    private async Task UploadDocumentAsync(
    string accessToken,
    Guid applicationId,
    string documentType)
    {
        var testPng =
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
                testPng);

        file.Headers.ContentType =
            new MediaTypeHeaderValue(
                "image/png");

        multipart.Add(
            file,
            "File",
            "workflow-identity-document.png");

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

    private async Task<ApplicationResponse>
    SubmitAsync(
        string accessToken,
        ApplicationResponse application)
    {
        /*
            Prepare the Birth Certificate Re-Issue application
            before submitting it.

            Required details:
                CurrentCertificateNumber
                RequestReason

            Required document:
                IDENTITY_DOCUMENT
        */

        await SaveBirthReissueDetailsAsync(
            accessToken,
            application.ApplicationId);

        await UploadDocumentAsync(
            accessToken,
            application.ApplicationId,
            "IDENTITY_DOCUMENT");

        using var request =
            AuthenticatedRequest(
                HttpMethod.Post,
                $"/api/v1/applications/" +
                $"{application.ApplicationId}/submit",
                accessToken);

        request.Content =
            JsonContent.Create(
                new ApplicationActionRequest(
                    application.RowVersion));

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var submitted =
            await response.Content
                .ReadFromJsonAsync<
                    ApplicationResponse>();

        submitted.Should().NotBeNull();

        return submitted!;
    }

    private async Task AssignAsync(
        string accessToken,
        Guid practitionerId,
        ApplicationResponse application)
    {
        var request =
            AuthenticatedRequest(
                HttpMethod.Put,
                $"/api/v1/applications/" +
                $"{application.ApplicationId}/assignment",
                accessToken);

        request.Content =
            JsonContent.Create(
                new AssignApplicationRequest(
                    practitionerId,
                    application.RowVersion,
                    "Workflow execution assignment."));

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);
    }

    private async Task<ApplicationResponse>
        ProcessAsync(
            string accessToken,
            ApplicationResponse application,
            string operation,
            string reason)
    {
        var request =
            AuthenticatedRequest(
                HttpMethod.Post,
                $"/api/v1/applications/" +
                $"{application.ApplicationId}/" +
                $"{operation}",
                accessToken);

        request.Content =
            JsonContent.Create(
                new ProcessApplicationRequest(
                    application.RowVersion,
                    reason));

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        return
            (await response.Content
                .ReadFromJsonAsync<
                    ApplicationResponse>())!;
    }

    private async Task<ApplicationResponse>
        DecideAsync(
            string accessToken,
            ApplicationResponse application,
            string operation,
            string reason)
    {
        var request =
            AuthenticatedRequest(
                HttpMethod.Post,
                $"/api/v1/applications/" +
                $"{application.ApplicationId}/" +
                $"{operation}",
                accessToken);

        request.Content =
            JsonContent.Create(
                new ApplicationDecisionRequest(
                    application.RowVersion,
                    reason));

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        return
            (await response.Content
                .ReadFromJsonAsync<
                    ApplicationResponse>())!;
    }

    private async Task<ApplicationResponse>
        GetApplicationAsync(
            string accessToken,
            Guid applicationId)
    {
        using var request =
            AuthenticatedRequest(
                HttpMethod.Get,
                $"/api/v1/applications/" +
                $"{applicationId}",
                accessToken);

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        return
            (await response.Content
                .ReadFromJsonAsync<
                    ApplicationResponse>())!;
    }

    private async Task<WorkflowInstanceResponse>
        GetWorkflowAsync(
            string accessToken,
            Guid applicationId)
    {
        using var request =
            AuthenticatedRequest(
                HttpMethod.Get,
                $"/api/v1/workflows/applications/" +
                $"{applicationId}",
                accessToken);

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        return
            (await response.Content
                .ReadFromJsonAsync<
                    WorkflowInstanceResponse>())!;
    }

    private static HttpRequestMessage
        AuthenticatedRequest(
            HttpMethod method,
            string uri,
            string token)
    {
        var request =
            new HttpRequestMessage(
                method,
                uri);

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);

        return request;
    }
}
