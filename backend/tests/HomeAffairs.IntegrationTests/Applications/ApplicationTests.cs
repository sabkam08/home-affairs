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

        var createResponse =
            await CreateApplicationAsync(
                citizen.AccessToken,
                new CreateApplicationRequest(
                    "SMART_ID_NEW",
                    null));

        var created =
            await createResponse.Content
                .ReadFromJsonAsync<ApplicationResponse>();

        created.Should().NotBeNull();

        var response =
            await SubmitApplicationAsync(
                citizen.AccessToken,
                created!.ApplicationId,
                created.RowVersion);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var submitted =
            await response.Content
                .ReadFromJsonAsync<ApplicationResponse>();

        submitted.Should().NotBeNull();

        submitted!.Status
            .Should()
            .Be("SUBMITTED");

        submitted.SubmittedAtUtc
            .Should()
            .NotBeNull();

        submitted.RowVersion
            .Should()
            .NotBe(created.RowVersion);
    }

    [Fact]
    public async Task Submitted_application_should_not_be_editable_as_draft()
    {
        var citizen =
            await CreateAdultCitizenAsync();

        var createResponse =
            await CreateApplicationAsync(
                citizen.AccessToken,
                new CreateApplicationRequest(
                    "SMART_ID_NEW",
                    null));

        var created =
            await createResponse.Content
                .ReadFromJsonAsync<ApplicationResponse>();

        created.Should().NotBeNull();

        var submitResponse =
            await SubmitApplicationAsync(
                citizen.AccessToken,
                created!.ApplicationId,
                created.RowVersion);

        var submitted =
            await submitResponse.Content
                .ReadFromJsonAsync<ApplicationResponse>();

        submitted.Should().NotBeNull();

        var update =
            await UpdateApplicationAsync(
                citizen.AccessToken,
                submitted!.ApplicationId,
                new UpdateApplicationRequest(
                    "SMART_ID_CORRECTION",
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

        var createResponse =
            await CreateApplicationAsync(
                citizen.AccessToken,
                new CreateApplicationRequest(
                    "SMART_ID_NEW",
                    null));

        var created =
            await createResponse.Content
                .ReadFromJsonAsync<ApplicationResponse>();

        created.Should().NotBeNull();

        await SubmitApplicationAsync(
            citizen.AccessToken,
            created!.ApplicationId,
            created.RowVersion);

        using var historyRequest =
            AuthenticatedRequest(
                HttpMethod.Get,
                $"/api/v1/applications/" +
                $"{created.ApplicationId}/history",
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
        var create =
            await CreateApplicationAsync(
                citizen.AccessToken,
                new CreateApplicationRequest(
                    "SMART_ID_CORRECTION",
                    null));

        var draft =
            await create.Content
                .ReadFromJsonAsync<ApplicationResponse>();

        draft.Should().NotBeNull();

        var submit =
            await SubmitApplicationAsync(
                citizen.AccessToken,
                draft!.ApplicationId,
                draft.RowVersion);

        var submitted =
            await submit.Content
                .ReadFromJsonAsync<ApplicationResponse>();

        submitted.Should().NotBeNull();

        return submitted!;
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
}
