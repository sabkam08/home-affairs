using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HomeAffairs.Contracts.Identity;
using HomeAffairs.IntegrationTests.Infrastructure;

namespace HomeAffairs.IntegrationTests.Identity;

[Collection(IntegrationTestCollection.Name)]
public sealed class RefreshTokenTests(
    HomeAffairsApiFactory factory)
{
    [Fact]
    public async Task Refresh_should_rotate_token()
    {
        var registered =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        var response =
            await factory.Client.PostAsJsonAsync(
                "/api/v1/auth/refresh",
                new RefreshTokenRequest(
                    registered.RefreshToken));

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var refreshed =
            await response.Content.ReadFromJsonAsync<
                TestAuthenticationResponse>();

        refreshed.Should().NotBeNull();

        refreshed!.AccessToken
            .Should()
            .NotBeNullOrWhiteSpace();

        refreshed.RefreshToken
            .Should()
            .NotBeNullOrWhiteSpace();

        refreshed.RefreshToken
            .Should()
            .NotBe(registered.RefreshToken);

        var revokedCount =
            await factory.QueryScalarAsync<int>(
                $"""
                 SELECT COUNT(1)
                 FROM [identity].RefreshTokens
                 WHERE UserId = '{registered.UserId}'
                   AND RevokedAtUtc IS NOT NULL
                   AND ReplacedByTokenId IS NOT NULL;
                 """);

        revokedCount
            .Should()
            .BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Reusing_rotated_token_should_revoke_active_family()
    {
        var registered =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        var firstRefreshResponse =
            await factory.Client.PostAsJsonAsync(
                "/api/v1/auth/refresh",
                new RefreshTokenRequest(
                    registered.RefreshToken));

        firstRefreshResponse.EnsureSuccessStatusCode();

        var firstRefresh =
            await firstRefreshResponse.Content
                .ReadFromJsonAsync<
                    TestAuthenticationResponse>();

        firstRefresh.Should().NotBeNull();

        var reuseResponse =
            await factory.Client.PostAsJsonAsync(
                "/api/v1/auth/refresh",
                new RefreshTokenRequest(
                    registered.RefreshToken));

        reuseResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.Unauthorized);

        var replacementResponse =
            await factory.Client.PostAsJsonAsync(
                "/api/v1/auth/refresh",
                new RefreshTokenRequest(
                    firstRefresh!.RefreshToken));

        replacementResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.Unauthorized);

        var eventCount =
            await factory.QueryScalarAsync<int>(
                $"""
                 SELECT COUNT(1)
                 FROM [identity].SecurityEvents
                 WHERE UserId = '{registered.UserId}'
                   AND EventType =
                       'REFRESH_TOKEN_REUSE_DETECTED';
                 """);

        eventCount.Should().BeGreaterThanOrEqualTo(1);
    }
}
