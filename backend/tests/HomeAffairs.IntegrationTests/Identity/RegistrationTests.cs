using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HomeAffairs.Contracts.Identity;
using HomeAffairs.IntegrationTests.Infrastructure;

namespace HomeAffairs.IntegrationTests.Identity;

[Collection(IntegrationTestCollection.Name)]
public sealed class RegistrationTests(
    HomeAffairsApiFactory factory)
{
    [Fact]
    public async Task Register_should_create_citizen()
    {
        var email =
            $"citizen-{Guid.NewGuid():N}@example.com";

        var request = new RegisterRequest(
            email,
            "HomeAffairs-Test-2026!",
            "Integration",
            "Citizen");

        var response =
            await factory.Client.PostAsJsonAsync(
                "/api/v1/auth/register",
                request);

        if (response.StatusCode !=
            HttpStatusCode.Created)
        {
            var error =
                await response.Content.ReadAsStringAsync();

            throw new Xunit.Sdk.XunitException(
                $"""
             Registration returned
             {(int)response.StatusCode}
             {response.StatusCode}.

             Response:
             {error}
             """);
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    AuthenticationResponse>();

        result.Should().NotBeNull();

        result!.Email
            .Should()
            .Be(email);

        result.Roles
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .Be("CITIZEN");

        result.AccessToken
            .Should()
            .NotBeNullOrWhiteSpace();

        result.RefreshToken
            .Should()
            .NotBeNullOrWhiteSpace();

        result.AccessTokenExpiresAtUtc
            .Should()
            .BeAfter(DateTimeOffset.UtcNow);

        result.RefreshTokenExpiresAtUtc
            .Should()
            .BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Duplicate_email_should_be_rejected()
    {
        var email =
            $"duplicate-{Guid.NewGuid():N}@example.com";

        var request = new RegisterRequest(
            email,
            "HomeAffairs-Test-2026!",
            "Integration",
            "Citizen");

        var first =
            await factory.Client.PostAsJsonAsync(
                "/api/v1/auth/register",
                request);

        first.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var second =
            await factory.Client.PostAsJsonAsync(
                "/api/v1/auth/register",
                request);

        second.StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_should_persist_user_role_and_refresh_token()
    {
        var email =
            $"persistence-{Guid.NewGuid():N}@example.com";

        var request = new RegisterRequest(
            email,
            "HomeAffairs-Test-2026!",
            "Persistence",
            "Test");

        var response =
            await factory.Client.PostAsJsonAsync(
                "/api/v1/auth/register",
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    AuthenticationResponse>();

        result.Should().NotBeNull();

        var userCount =
            await factory.QueryScalarAsync<int>(
                $"""
             SELECT COUNT(1)
             FROM [identity].Users
             WHERE UserId = '{result!.UserId}'
               AND Email = '{email}'
               AND IsActive = 1;
             """);

        userCount.Should().Be(1);

        var citizenRoleCount =
            await factory.QueryScalarAsync<int>(
                $"""
             SELECT COUNT(1)
             FROM [identity].UserRoles AS ur
             INNER JOIN [identity].Roles AS r
                 ON r.RoleId = ur.RoleId
             WHERE ur.UserId = '{result.UserId}'
               AND r.Code = 'CITIZEN';
             """);

        citizenRoleCount.Should().Be(1);

        var refreshTokenCount =
            await factory.QueryScalarAsync<int>(
                $"""
             SELECT COUNT(1)
             FROM [identity].RefreshTokens
             WHERE UserId = '{result.UserId}'
               AND RevokedAtUtc IS NULL;
             """);

        refreshTokenCount.Should().Be(1);

        var registrationEventCount =
            await factory.QueryScalarAsync<int>(
                $"""
             SELECT COUNT(1)
             FROM [identity].SecurityEvents
             WHERE UserId = '{result.UserId}'
               AND EventType = 'USER_REGISTERED';
             """);

        registrationEventCount.Should().Be(1);
    }
}
