using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HomeAffairs.Contracts.Applications;
using HomeAffairs.Contracts.Citizens;
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
}
