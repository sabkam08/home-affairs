using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HomeAffairs.Contracts.Applications;
using HomeAffairs.Contracts.Citizens;
using HomeAffairs.Contracts.Workflow;
using HomeAffairs.IntegrationTests.Identity;
using HomeAffairs.IntegrationTests.Infrastructure;

namespace HomeAffairs.IntegrationTests.Workflow;

[Collection(IntegrationTestCollection.Name)]
public sealed class WorkflowInstanceTests(
    HomeAffairsApiFactory factory)
{
    [Fact]
    public async Task New_application_should_create_workflow_instance_atomically()
    {
        var citizen =
            await CreateCitizenAsync();

        var application =
            await CreateApplicationAsync(
                citizen.AccessToken,
                "SMART_ID_CORRECTION");

        var workflow =
            await GetWorkflowInstanceAsync(
                citizen.AccessToken,
                application.ApplicationId);

        workflow.ApplicationId
            .Should()
            .Be(application.ApplicationId);

        workflow.ApplicationType
            .Should()
            .Be(application.ApplicationType);

        workflow.CurrentStateCode
            .Should()
            .Be("DRAFT");

        workflow.CurrentStateCode
            .Should()
            .Be(application.Status);

        workflow.WorkflowVersion
            .Should()
            .Be(1);

        workflow.CompletedAtUtc
            .Should()
            .BeNull();

        workflow.RowVersion
            .Should()
            .NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Application_should_have_exactly_one_workflow_instance()
    {
        var citizen =
            await CreateCitizenAsync();

        var application =
            await CreateApplicationAsync(
                citizen.AccessToken,
                "SMART_ID_NEW");

        var count =
            await factory.QueryScalarAsync<int>(
                $"""
                 SELECT COUNT(1)
                 FROM [workflow].WorkflowInstances
                 WHERE ApplicationId =
                     '{application.ApplicationId}';
                 """);

        count.Should().Be(1);
    }

    [Fact]
    public async Task Workflow_instance_should_pin_active_definition()
    {
        var citizen =
            await CreateCitizenAsync();

        var application =
            await CreateApplicationAsync(
                citizen.AccessToken,
                "SMART_ID_REPLACEMENT_LOST");

        var workflow =
            await GetWorkflowInstanceAsync(
                citizen.AccessToken,
                application.ApplicationId);

        var activeDefinitionId =
            await factory.QueryScalarAsync<Guid>(
                $"""
                 SELECT WorkflowDefinitionId
                 FROM [workflow].WorkflowDefinitions
                 WHERE ApplicationType =
                     'SMART_ID_REPLACEMENT_LOST'
                   AND IsActive = 1;
                 """);

        workflow.WorkflowDefinitionId
            .Should()
            .Be(activeDefinitionId);
    }

    [Fact]
    public async Task Draft_workflow_should_return_submit_and_cancel_actions()
    {
        var citizen =
            await CreateCitizenAsync();

        var application =
            await CreateApplicationAsync(
                citizen.AccessToken,
                "SMART_ID_CORRECTION");

        var transitions =
            await GetTransitionsAsync(
                citizen.AccessToken,
                application.ApplicationId);

        transitions
            .Select(transition =>
                transition.ActionCode)
            .Should()
            .BeEquivalentTo(
                "SUBMIT",
                "CANCEL");

        transitions
            .Should()
            .OnlyContain(transition =>
                transition.FromStateCode ==
                "DRAFT");
    }

    [Fact]
    public async Task Other_citizen_should_not_read_workflow_instance()
    {
        var owner =
            await CreateCitizenAsync();

        var application =
            await CreateApplicationAsync(
                owner.AccessToken,
                "SMART_ID_CORRECTION");

        var other =
            await CreateCitizenAsync();

        using var request =
            CreateAuthenticatedRequest(
                HttpMethod.Get,
                $"/api/v1/workflows/applications/" +
                $"{application.ApplicationId}",
                other.AccessToken);

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Other_citizen_should_not_read_available_transitions()
    {
        var owner =
            await CreateCitizenAsync();

        var application =
            await CreateApplicationAsync(
                owner.AccessToken,
                "SMART_ID_CORRECTION");

        var other =
            await CreateCitizenAsync();

        using var request =
            CreateAuthenticatedRequest(
                HttpMethod.Get,
                $"/api/v1/workflows/applications/" +
                $"{application.ApplicationId}/transitions",
                other.AccessToken);

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Application_and_workflow_state_should_match_after_creation()
    {
        var citizen =
            await CreateCitizenAsync();

        var application =
            await CreateApplicationAsync(
                citizen.AccessToken,
                "BIRTH_CERTIFICATE_REISSUE");

        var mismatchCount =
            await factory.QueryScalarAsync<int>(
                $"""
                 SELECT COUNT(1)
                 FROM [applications].Applications AS a
                 INNER JOIN [workflow].WorkflowInstances AS wi
                     ON wi.ApplicationId = a.ApplicationId
                 WHERE a.ApplicationId =
                     '{application.ApplicationId}'
                   AND a.Status <>
                     wi.CurrentStateCode;
                 """);

        mismatchCount
            .Should()
            .Be(0);
    }

    private async Task<TestAuthenticationResponse>
        CreateCitizenAsync()
    {
        var identity =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        var request =
            CreateAuthenticatedRequest(
                HttpMethod.Post,
                "/api/v1/citizens/me",
                identity.AccessToken);

        request.Content =
            JsonContent.Create(
                new CreateCitizenProfileRequest(
                    null,
                    "Workflow",
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

    private async Task<ApplicationResponse>
        CreateApplicationAsync(
            string accessToken,
            string applicationType)
    {
        var request =
            CreateAuthenticatedRequest(
                HttpMethod.Post,
                "/api/v1/applications",
                accessToken);

        request.Content =
            JsonContent.Create(
                new CreateApplicationRequest(
                    applicationType,
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

    private async Task<WorkflowInstanceResponse>
        GetWorkflowInstanceAsync(
            string accessToken,
            Guid applicationId)
    {
        using var request =
            CreateAuthenticatedRequest(
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

        var workflow =
            await response.Content
                .ReadFromJsonAsync<
                    WorkflowInstanceResponse>();

        workflow.Should().NotBeNull();

        return workflow!;
    }

    private async Task<
        AvailableWorkflowTransitionResponse[]>
        GetTransitionsAsync(
            string accessToken,
            Guid applicationId)
    {
        using var request =
            CreateAuthenticatedRequest(
                HttpMethod.Get,
                $"/api/v1/workflows/applications/" +
                $"{applicationId}/transitions",
                accessToken);

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var transitions =
            await response.Content
                .ReadFromJsonAsync<
                    AvailableWorkflowTransitionResponse[]>();

        transitions.Should().NotBeNull();

        return transitions!;
    }

    private static HttpRequestMessage
        CreateAuthenticatedRequest(
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
}
