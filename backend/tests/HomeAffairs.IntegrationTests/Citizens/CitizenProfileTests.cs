using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HomeAffairs.Contracts.Citizens;
using HomeAffairs.IntegrationTests.Identity;
using HomeAffairs.IntegrationTests.Infrastructure;

namespace HomeAffairs.IntegrationTests.Citizens;

[Collection(IntegrationTestCollection.Name)]
public sealed class CitizenProfileTests(
    HomeAffairsApiFactory factory)
{
    [Fact]
    public async Task Citizen_should_create_and_read_own_profile()
    {
        var identity =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        var createRequest =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/v1/citizens/me");

        createRequest.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                identity.AccessToken);

        createRequest.Content =
            JsonContent.Create(
                new CreateCitizenProfileRequest(
                    null,
                    "Integration",
                    "Citizen",
                    "Profile",
                    new DateOnly(
                        1995,
                        6,
                        15),
                    "F"));

        var createResponse =
            await factory.Client.SendAsync(
                createRequest);

        createResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var created =
            await createResponse.Content
                .ReadFromJsonAsync<
                    CitizenProfileResponse>();

        created.Should().NotBeNull();

        using var getRequest =
            IdentityTestHelper
                .CreateAuthenticatedRequest(
                    HttpMethod.Get,
                    "/api/v1/citizens/me",
                    identity.AccessToken);

        var getResponse =
            await factory.Client.SendAsync(
                getRequest);

        getResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var loaded =
            await getResponse.Content
                .ReadFromJsonAsync<
                    CitizenProfileResponse>();

        loaded.Should().NotBeNull();

        loaded!.CitizenId
            .Should()
            .Be(created!.CitizenId);

        loaded.FirstName
            .Should()
            .Be("Integration");
    }

    [Fact]
    public async Task User_should_not_create_second_profile()
    {
        var identity =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        var request =
            new CreateCitizenProfileRequest(
                null,
                "Duplicate",
                null,
                "Citizen",
                new DateOnly(
                    1990,
                    1,
                    1),
                "M");

        async Task<HttpResponseMessage>
            CreateProfileAsync()
        {
            var message =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    "/api/v1/citizens/me");

            message.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    identity.AccessToken);

            message.Content =
                JsonContent.Create(request);

            return await factory.Client
                .SendAsync(message);
        }

        var first =
            await CreateProfileAsync();

        var second =
            await CreateProfileAsync();

        first.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        second.StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);
    }
}
