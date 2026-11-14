using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HomeAffairs.Contracts.Applications;
using HomeAffairs.Contracts.Citizens;
using HomeAffairs.Contracts.SmartId;
using HomeAffairs.IntegrationTests.Identity;
using HomeAffairs.IntegrationTests.Infrastructure;

namespace HomeAffairs.IntegrationTests.SmartId;

[Collection(IntegrationTestCollection.Name)]
public sealed class SmartIdServiceTests(
    HomeAffairsApiFactory factory)
{
    [Theory]
    [InlineData("SMART_ID_NEW")]
    [InlineData("SMART_ID_REPLACEMENT_LOST")]
    [InlineData("SMART_ID_REPLACEMENT_STOLEN")]
    [InlineData("SMART_ID_REPLACEMENT_DAMAGED")]
    [InlineData("SMART_ID_CORRECTION")]
    public async Task Every_smart_id_type_should_accept_service_details(
        string applicationType)
    {
        var setup =
            await CreateApplicationAsync(
                applicationType);

        var response =
            await SaveAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                CreateRequest());

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var details =
            await response.Content
                .ReadFromJsonAsync<
                    SmartIdServiceDetailsResponse>();

        details.Should().NotBeNull();

        details!.ApplicationType
            .Should()
            .Be(applicationType);

        details.RowVersion
            .Should()
            .NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Applicant_should_save_smart_id_correction_details()
    {
        var setup =
            await CreateApplicationAsync(
                "SMART_ID_CORRECTION");

        var request =
            CreateRequest() with
            {
                CorrectionField =
                    "LAST_NAME",

                CurrentValue =
                    "Old Surname",

                RequestedValue =
                    "New Surname",

                RequestReason =
                    "Surname changed after marriage."
            };

        var response =
            await SaveAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var details =
            await response.Content
                .ReadFromJsonAsync<
                    SmartIdServiceDetailsResponse>();

        details.Should().NotBeNull();

        details!.CorrectionField
            .Should()
            .Be("LAST_NAME");

        details.CurrentValue
            .Should()
            .Be("Old Surname");

        details.RequestedValue
            .Should()
            .Be("New Surname");
    }

    [Fact]
    public async Task Applicant_should_read_saved_smart_id_details()
    {
        var setup =
            await CreateApplicationAsync(
                "SMART_ID_REPLACEMENT_STOLEN");

        await SaveAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            CreateRequest());

        using var request =
            Authenticated(
                HttpMethod.Get,
                $"/api/v1/applications/" +
                $"{setup.Application.ApplicationId}/" +
                "smart-id",
                setup.Identity.AccessToken);

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var details =
            await response.Content
                .ReadFromJsonAsync<
                    SmartIdServiceDetailsResponse>();

        details.Should().NotBeNull();

        details!.ExistingSmartIdNumber
            .Should()
            .Be("9001015009087");

        details.PoliceCaseNumber
            .Should()
            .Be("CAS-123-11-2026");
    }

    [Fact]
    public async Task Applicant_should_update_smart_id_details_with_current_row_version()
    {
        var setup =
            await CreateApplicationAsync(
                "SMART_ID_CORRECTION");

        var firstResponse =
            await SaveAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                CreateRequest());

        var first =
            await firstResponse.Content
                .ReadFromJsonAsync<
                    SmartIdServiceDetailsResponse>();

        first.Should().NotBeNull();

        var updateRequest =
            CreateRequest(
                first!.RowVersion) with
            {
                RequestedValue =
                    "Updated Requested Value"
            };

        var updateResponse =
            await SaveAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                updateRequest);

        updateResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var updated =
            await updateResponse.Content
                .ReadFromJsonAsync<
                    SmartIdServiceDetailsResponse>();

        updated.Should().NotBeNull();

        updated!.RequestedValue
            .Should()
            .Be("Updated Requested Value");

        updated.RowVersion
            .Should()
            .NotBe(first.RowVersion);
    }

    [Fact]
    public async Task Stale_smart_id_details_update_should_return_conflict()
    {
        var setup =
            await CreateApplicationAsync(
                "SMART_ID_CORRECTION");

        var firstResponse =
            await SaveAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                CreateRequest());

        var first =
            await firstResponse.Content
                .ReadFromJsonAsync<
                    SmartIdServiceDetailsResponse>();

        first.Should().NotBeNull();

        var oldRowVersion =
            first!.RowVersion;

        var currentUpdate =
            await SaveAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                CreateRequest(
                    oldRowVersion) with
                {
                    RequestedValue =
                        "First Update"
                });

        currentUpdate.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var staleUpdate =
            await SaveAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                CreateRequest(
                    oldRowVersion) with
                {
                    RequestedValue =
                        "Stale Update"
                });

        staleUpdate.StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Birth_application_should_reject_smart_id_details()
    {
        var setup =
            await CreateApplicationAsync(
                "BIRTH_REGISTRATION");

        var response =
            await SaveAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                CreateRequest());

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Other_citizen_should_not_read_smart_id_details()
    {
        var owner =
            await CreateApplicationAsync(
                "SMART_ID_CORRECTION");

        await SaveAsync(
            owner.Identity.AccessToken,
            owner.Application.ApplicationId,
            CreateRequest());

        var other =
            await CreateCitizenAsync();

        using var request =
            Authenticated(
                HttpMethod.Get,
                $"/api/v1/applications/" +
                $"{owner.Application.ApplicationId}/" +
                "smart-id",
                other.AccessToken);

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Other_citizen_should_not_update_smart_id_details()
    {
        var owner =
            await CreateApplicationAsync(
                "SMART_ID_CORRECTION");

        var savedResponse =
            await SaveAsync(
                owner.Identity.AccessToken,
                owner.Application.ApplicationId,
                CreateRequest());

        var saved =
            await savedResponse.Content
                .ReadFromJsonAsync<
                    SmartIdServiceDetailsResponse>();

        saved.Should().NotBeNull();

        var other =
            await CreateCitizenAsync();

        var response =
            await SaveAsync(
                other.AccessToken,
                owner.Application.ApplicationId,
                CreateRequest(
                    saved!.RowVersion));

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Submitted_smart_id_application_should_not_allow_detail_changes()
    {
        var setup =
            await CreateApplicationAsync(
                "SMART_ID_CORRECTION");

        var savedResponse =
            await SaveAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                CreateRequest());

        var saved =
            await savedResponse.Content
                .ReadFromJsonAsync<
                    SmartIdServiceDetailsResponse>();

        saved.Should().NotBeNull();

        /*
            Smart ID submission validation is introduced
            in 11C, so current generic submission remains
            available in 11A.
        */

        using var submitRequest =
            Authenticated(
                HttpMethod.Post,
                $"/api/v1/applications/" +
                $"{setup.Application.ApplicationId}/submit",
                setup.Identity.AccessToken);

        submitRequest.Content =
            JsonContent.Create(
                new ApplicationActionRequest(
                    setup.Application.RowVersion));

        var submitResponse =
            await factory.Client.SendAsync(
                submitRequest);

        submitResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var update =
            await SaveAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                CreateRequest(
                    saved!.RowVersion));

        update.StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Smart_id_details_should_be_stored_once_per_application()
    {
        var setup =
            await CreateApplicationAsync(
                "SMART_ID_CORRECTION");

        var firstResponse =
            await SaveAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                CreateRequest());

        var first =
            await firstResponse.Content
                .ReadFromJsonAsync<
                    SmartIdServiceDetailsResponse>();

        first.Should().NotBeNull();

        await SaveAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            CreateRequest(
                first!.RowVersion) with
            {
                RequestedValue =
                    "Updated"
            });

        var count =
            await factory.QueryScalarAsync<int>(
                $"""
                 SELECT COUNT(1)
                 FROM [smartid].SmartIdServiceDetails
                 WHERE ApplicationId =
                     '{setup.Application.ApplicationId}';
                 """);

        count.Should().Be(1);
    }

    [Fact]
    public async Task Citizen_should_not_create_second_active_smart_id_application()
    {
        var identity =
            await CreateCitizenAsync();

        var first =
            await CreateApplicationForCitizenAsync(
                identity,
                "SMART_ID_NEW");

        first.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var second =
            await CreateApplicationForCitizenAsync(
                identity,
                "SMART_ID_NEW");

        second.StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Different_smart_id_type_should_still_be_blocked_for_same_subject()
    {
        var identity =
            await CreateCitizenAsync();

        var first =
            await CreateApplicationForCitizenAsync(
                identity,
                "SMART_ID_NEW");

        first.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var second =
            await CreateApplicationForCitizenAsync(
                identity,
                "SMART_ID_REPLACEMENT_LOST");

        second.StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Birth_application_should_not_block_smart_id_application()
    {
        var identity =
            await CreateCitizenAsync();

        var birth =
            await CreateApplicationForCitizenAsync(
                identity,
                "BIRTH_CERTIFICATE_REISSUE");

        birth.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var smartId =
            await CreateApplicationForCitizenAsync(
                identity,
                "SMART_ID_NEW");

        smartId.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Different_citizens_should_have_independent_smart_id_slots()
    {
        var firstCitizen =
            await CreateCitizenAsync();

        var secondCitizen =
            await CreateCitizenAsync();

        var first =
            await CreateApplicationForCitizenAsync(
                firstCitizen,
                "SMART_ID_NEW");

        var second =
            await CreateApplicationForCitizenAsync(
                secondCitizen,
                "SMART_ID_NEW");

        first.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        second.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Cancelled_smart_id_application_should_release_subject_slot()
    {
        var identity =
            await CreateCitizenAsync();

        var firstResponse =
            await CreateApplicationForCitizenAsync(
                identity,
                "SMART_ID_NEW");

        var first =
            await firstResponse.Content
                .ReadFromJsonAsync<
                    ApplicationResponse>();

        first.Should().NotBeNull();

        using var cancelRequest =
            Authenticated(
                HttpMethod.Post,
                $"/api/v1/applications/" +
                $"{first!.ApplicationId}/cancel",
                identity.AccessToken);

        cancelRequest.Content =
            JsonContent.Create(
                new CancelApplicationRequest(
                    first.RowVersion,
                    "Applicant changed request."));

        var cancelResponse =
            await factory.Client.SendAsync(
                cancelRequest);

        cancelResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var second =
            await CreateApplicationForCitizenAsync(
                identity,
                "SMART_ID_REPLACEMENT_LOST");

        second.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Applicant_and_dependant_should_have_independent_smart_id_slots()
    {
        var identity =
            await CreateCitizenAsync();

        var selfApplication =
            await CreateApplicationForCitizenAsync(
                identity,
                "SMART_ID_NEW");

        selfApplication.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var dependant =
            await CreateDependantAsync(
                identity.AccessToken);

        var dependantApplication =
            await CreateApplicationForCitizenAsync(
                identity,
                "SMART_ID_NEW",
                dependant.SubjectCitizenId);

        dependantApplication.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Same_dependant_should_not_have_two_active_smart_id_applications()
    {
        var identity =
            await CreateCitizenAsync();

        var dependant =
            await CreateDependantAsync(
                identity.AccessToken);

        var first =
            await CreateApplicationForCitizenAsync(
                identity,
                "SMART_ID_NEW",
                dependant.SubjectCitizenId);

        first.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var second =
            await CreateApplicationForCitizenAsync(
                identity,
                "SMART_ID_CORRECTION",
                dependant.SubjectCitizenId);

        second.StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Concurrent_smart_id_creation_should_allow_only_one_active_application()
    {
        var identity =
            await CreateCitizenAsync();

        var firstTask =
            CreateApplicationForCitizenAsync(
                identity,
                "SMART_ID_NEW");

        var secondTask =
            CreateApplicationForCitizenAsync(
                identity,
                "SMART_ID_REPLACEMENT_LOST");

        var responses =
            await Task.WhenAll(
                firstTask,
                secondTask);

        responses
            .Count(response =>
                response.StatusCode ==
                HttpStatusCode.Created)
            .Should()
            .Be(1);

        responses
            .Count(response =>
                response.StatusCode ==
                HttpStatusCode.Conflict)
            .Should()
            .Be(1);

        var citizenId =
            await factory.QueryScalarAsync<Guid>(
                $"""
             SELECT CitizenId
             FROM [citizens].Citizens
             WHERE UserId =
                 '{identity.UserId}';
             """);

        var activeCount =
            await factory.QueryScalarAsync<int>(
                $"""
             SELECT COUNT(1)
             FROM [applications].Applications
             WHERE SubjectCitizenId =
                 '{citizenId}'
               AND ApplicationType IN
               (
                   'SMART_ID_NEW',
                   'SMART_ID_REPLACEMENT_LOST',
                   'SMART_ID_REPLACEMENT_STOLEN',
                   'SMART_ID_REPLACEMENT_DAMAGED',
                   'SMART_ID_CORRECTION'
               )
               AND Status IN
               (
                   'DRAFT',
                   'SUBMITTED',
                   'IN_PROGRESS',
                   'AWAITING_INFORMATION',
                   'AWAITING_APPROVAL'
               );
             """);

        activeCount
            .Should()
            .Be(1);
    }

    #region Helpers
    private sealed record Setup(
        TestAuthenticationResponse Identity,
        ApplicationResponse Application);

    private async Task<Setup>
        CreateApplicationAsync(
            string applicationType)
    {
        var identity =
            await CreateCitizenAsync();

        using var request =
            Authenticated(
                HttpMethod.Post,
                "/api/v1/applications",
                identity.AccessToken);

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

        return new Setup(
            identity,
            application!);
    }

    private async Task<TestAuthenticationResponse>
        CreateCitizenAsync()
    {
        var identity =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        using var request =
            Authenticated(
                HttpMethod.Post,
                "/api/v1/citizens/me",
                identity.AccessToken);

        request.Content =
            JsonContent.Create(
                new CreateCitizenProfileRequest(
                    null,
                    "Smart",
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

    private async Task<HttpResponseMessage>
        SaveAsync(
            string token,
            Guid applicationId,
            SaveSmartIdServiceDetailsRequest body)
    {
        using var request =
            Authenticated(
                HttpMethod.Put,
                $"/api/v1/applications/" +
                $"{applicationId}/smart-id",
                token);

        request.Content =
            JsonContent.Create(body);

        return await factory.Client.SendAsync(
            request);
    }

    private static SaveSmartIdServiceDetailsRequest
        CreateRequest(
            string? rowVersion = null)
    {
        return new SaveSmartIdServiceDetailsRequest(
            ExistingSmartIdNumber:
                "9001015009087",

            RequestedFirstName:
                "Test",

            RequestedMiddleNames:
                "Integration",

            RequestedLastName:
                "Citizen",

            RequestedDateOfBirth:
                new DateOnly(
                    1990,
                    1,
                    1),

            RequestedSex:
                "F",

            ReplacementReason:
                "Smart ID replacement required.",

            IncidentDate:
                new DateOnly(
                    2026,
                    11,
                    1),

            PoliceCaseNumber:
                "CAS-123-11-2026",

            PoliceStation:
                "Cape Town Central",

            DamageDescription:
                "Card data cannot be read.",

            CorrectionField:
                "LAST_NAME",

            CurrentValue:
                "Old Surname",

            RequestedValue:
                "New Surname",

            RequestReason:
                "Requested service-specific change.",

            RowVersion:
                rowVersion);
    }

    private static HttpRequestMessage
        Authenticated(
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

    private async Task<HttpResponseMessage>
    CreateApplicationForCitizenAsync(
        TestAuthenticationResponse identity,
        string applicationType,
        Guid? subjectCitizenId = null)
    {
        var request =
            Authenticated(
                HttpMethod.Post,
                "/api/v1/applications",
                identity.AccessToken);

        request.Content =
            JsonContent.Create(
                new CreateApplicationRequest(
                    applicationType,
                    subjectCitizenId));

        return await factory.Client.SendAsync(
            request);
    }

    private async Task<DependantResponse>
    CreateDependantAsync(
        string accessToken)
    {
        var request =
            Authenticated(
                HttpMethod.Post,
                "/api/v1/citizens/me/dependants",
                accessToken);

        request.Content =
            JsonContent.Create(
                new CreateDependantRequest(
                    null,
                    "Smart",
                    "ID",
                    "Dependant",
                    new DateOnly(
                        2015,
                        6,
                        10),
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
    #endregion
}
