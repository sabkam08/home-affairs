using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HomeAffairs.IntegrationTests.Infrastructure;

namespace HomeAffairs.IntegrationTests.Identity;

[Collection(IntegrationTestCollection.Name)]
public sealed class CurrentUserTests(
    HomeAffairsApiFactory factory)
{
    [Fact]
    public async Task Authenticated_user_should_read_own_profile()
    {
        var registered =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        using var request =
            IdentityTestHelper
                .CreateAuthenticatedRequest(
                    HttpMethod.Get,
                    "/api/v1/auth/me",
                    registered.AccessToken);

        var response =
            await factory.Client.SendAsync(request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var currentUser =
            await response.Content.ReadFromJsonAsync<
                TestCurrentUserResponse>();

        currentUser.Should().NotBeNull();

        currentUser!.UserId
            .Should()
            .Be(registered.UserId);

        currentUser.Email
            .Should()
            .Be(registered.Email);

        currentUser.Roles
            .Should()
            .Contain("CITIZEN");
    }

    [Fact]
    public async Task Anonymous_user_should_not_read_profile()
    {
        var response =
            await factory.Client.GetAsync(
                "/api/v1/auth/me");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Unauthorized);
    }
}
