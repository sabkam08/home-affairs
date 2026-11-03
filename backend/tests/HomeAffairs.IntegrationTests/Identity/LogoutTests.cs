using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HomeAffairs.Contracts.Identity;
using HomeAffairs.IntegrationTests.Infrastructure;

namespace HomeAffairs.IntegrationTests.Identity;

[Collection(IntegrationTestCollection.Name)]
public sealed class LogoutTests(
    HomeAffairsApiFactory factory)
{
    [Fact]
    public async Task Logout_should_revoke_refresh_token()
    {
        var registered =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        var logoutResponse =
            await factory.Client.PostAsJsonAsync(
                "/api/v1/auth/logout",
                new LogoutRequest(
                    registered.RefreshToken));

        logoutResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.NoContent);

        var refreshResponse =
            await factory.Client.PostAsJsonAsync(
                "/api/v1/auth/refresh",
                new RefreshTokenRequest(
                    registered.RefreshToken));

        refreshResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.Unauthorized);

        var revokedCount =
            await factory.QueryScalarAsync<int>(
                $"""
                 SELECT COUNT(1)
                 FROM [identity].RefreshTokens
                 WHERE UserId = '{registered.UserId}'
                   AND RevokedAtUtc IS NOT NULL;
                 """);

        revokedCount
            .Should()
            .BeGreaterThanOrEqualTo(1);

        var eventCount =
            await factory.QueryScalarAsync<int>(
                $"""
                 SELECT COUNT(1)
                 FROM [identity].SecurityEvents
                 WHERE UserId = '{registered.UserId}'
                   AND EventType = 'USER_LOGGED_OUT';
                 """);

        eventCount
            .Should()
            .BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Logout_should_be_idempotent()
    {
        var registered =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        var first =
            await factory.Client.PostAsJsonAsync(
                "/api/v1/auth/logout",
                new LogoutRequest(
                    registered.RefreshToken));

        var second =
            await factory.Client.PostAsJsonAsync(
                "/api/v1/auth/logout",
                new LogoutRequest(
                    registered.RefreshToken));

        first.StatusCode
            .Should()
            .Be(HttpStatusCode.NoContent);

        second.StatusCode
            .Should()
            .Be(HttpStatusCode.NoContent);
    }
}
