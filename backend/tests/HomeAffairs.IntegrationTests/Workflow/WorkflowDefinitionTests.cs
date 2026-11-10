using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HomeAffairs.Contracts.Workflow;
using HomeAffairs.IntegrationTests.Identity;
using HomeAffairs.IntegrationTests.Infrastructure;

namespace HomeAffairs.IntegrationTests.Workflow;

[Collection(IntegrationTestCollection.Name)]
public sealed class WorkflowDefinitionTests(
    HomeAffairsApiFactory factory)
{
    [Fact]
    public async Task Supported_application_type_should_return_workflow()
    {
        var citizen =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        var workflow =
            await GetWorkflowAsync(
                citizen.AccessToken,
                "SMART_ID_NEW");

        workflow.ApplicationType
            .Should()
            .Be("SMART_ID_NEW");

        workflow.Version
            .Should()
            .Be(1);

        workflow.IsActive
            .Should()
            .BeTrue();

        workflow.States
            .Should()
            .HaveCount(9);

        workflow.Transitions
            .Should()
            .HaveCount(8);
    }

    [Fact]
    public async Task Workflow_should_have_exactly_one_initial_state()
    {
        var citizen =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        var workflow =
            await GetWorkflowAsync(
                citizen.AccessToken,
                "SMART_ID_CORRECTION");

        workflow.States
            .Count(state =>
                state.IsInitial)
            .Should()
            .Be(1);

        workflow.States
            .Single(state =>
                state.IsInitial)
            .StateCode
            .Should()
            .Be("DRAFT");
    }

    [Fact]
    public async Task Workflow_should_define_terminal_states()
    {
        var citizen =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        var workflow =
            await GetWorkflowAsync(
                citizen.AccessToken,
                "BIRTH_CERTIFICATE_REISSUE");

        var terminalStates =
            workflow.States
                .Where(state =>
                    state.IsTerminal)
                .Select(state =>
                    state.StateCode)
                .ToArray();

        terminalStates
            .Should()
            .BeEquivalentTo(
                "APPROVED",
                "REJECTED",
                "COMPLETED",
                "CANCELLED");
    }

    [Fact]
    public async Task Workflow_should_define_permission_per_transition()
    {
        var citizen =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        var workflow =
            await GetWorkflowAsync(
                citizen.AccessToken,
                "SMART_ID_NEW");

        workflow.Transitions
            .Should()
            .OnlyContain(transition =>
                !string.IsNullOrWhiteSpace(
                    transition.RequiredPermission));

        var approval =
            workflow.Transitions.Single(
                transition =>
                    transition.ActionCode ==
                    "APPROVE");

        approval.RequiredPermission
            .Should()
            .Be("APPLICATION.APPROVE");
    }

    [Fact]
    public async Task Rejection_should_require_reason()
    {
        var citizen =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        var workflow =
            await GetWorkflowAsync(
                citizen.AccessToken,
                "SMART_ID_NEW");

        var rejection =
            workflow.Transitions.Single(
                transition =>
                    transition.ActionCode ==
                    "REJECT");

        rejection.RequiresReason
            .Should()
            .BeTrue();
    }

    [Theory]
    [InlineData("BIRTH_REGISTRATION")]
    [InlineData("BIRTH_CERTIFICATE_REISSUE")]
    [InlineData("LATE_BIRTH_REGISTRATION")]
    [InlineData("BIRTH_CERTIFICATE_AMENDMENT")]
    [InlineData("SMART_ID_NEW")]
    [InlineData("SMART_ID_REPLACEMENT_LOST")]
    [InlineData("SMART_ID_REPLACEMENT_STOLEN")]
    [InlineData("SMART_ID_REPLACEMENT_DAMAGED")]
    [InlineData("SMART_ID_CORRECTION")]
    public async Task Every_supported_application_type_should_have_active_workflow(
        string applicationType)
    {
        var citizen =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        var workflow =
            await GetWorkflowAsync(
                citizen.AccessToken,
                applicationType);

        workflow.ApplicationType
            .Should()
            .Be(applicationType);

        workflow.IsActive
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task Invalid_application_type_should_return_bad_request()
    {
        var citizen =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        using var request =
            CreateRequest(
                citizen.AccessToken,
                "UNKNOWN_APPLICATION_TYPE");

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Anonymous_user_should_not_read_workflow()
    {
        var response =
            await factory.Client.GetAsync(
                "/api/v1/workflows/SMART_ID_NEW");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Database_transition_check_should_match_seed_definition()
    {
        var allowed =
            await factory.QueryScalarAsync<bool>(
                """
                EXEC [workflow].WorkflowTransition_IsAllowed
                    @ApplicationType =
                        N'SMART_ID_NEW',
                    @FromStateCode =
                        N'IN_PROGRESS',
                    @ToStateCode =
                        N'AWAITING_APPROVAL';
                """);

        allowed.Should().BeTrue();

        var forbidden =
            await factory.QueryScalarAsync<bool>(
                """
                EXEC [workflow].WorkflowTransition_IsAllowed
                    @ApplicationType =
                        N'SMART_ID_NEW',
                    @FromStateCode =
                        N'DRAFT',
                    @ToStateCode =
                        N'APPROVED';
                """);

        forbidden.Should().BeFalse();
    }

    private async Task<WorkflowDefinitionResponse>
        GetWorkflowAsync(
            string accessToken,
            string applicationType)
    {
        using var request =
            CreateRequest(
                accessToken,
                applicationType);

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var workflow =
            await response.Content
                .ReadFromJsonAsync<
                    WorkflowDefinitionResponse>();

        workflow.Should().NotBeNull();

        return workflow!;
    }

    private static HttpRequestMessage
        CreateRequest(
            string accessToken,
            string applicationType)
    {
        var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/v1/workflows/" +
                $"{applicationType}");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        return request;
    }
}
