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

        if (response.StatusCode != HttpStatusCode.Created)
        {
            var responseBody =
                await response.Content.ReadAsStringAsync();

            var userCount =
                await factory.QueryScalarAsync<int>(
                    $"""
                 SELECT COUNT(1)
                 FROM [identity].Users
                 WHERE Email = '{email}';
                 """);

            var activeUserCount =
                await factory.QueryScalarAsync<int>(
                    $"""
                 SELECT COUNT(1)
                 FROM [identity].Users
                 WHERE Email = '{email}'
                   AND IsActive = 1;
                 """);

            var roleCount =
                await factory.QueryScalarAsync<int>(
                    $"""
                 SELECT COUNT(1)
                 FROM [identity].Users AS u
                 INNER JOIN [identity].UserRoles AS ur
                     ON ur.UserId = u.UserId
                 WHERE u.Email = '{email}';
                 """);

            var securityEventCount =
                await factory.QueryScalarAsync<int>(
                    $"""
                 SELECT COUNT(1)
                 FROM [identity].SecurityEvents AS se
                 INNER JOIN [identity].Users AS u
                     ON u.UserId = se.UserId
                 WHERE u.Email = '{email}';
                 """);

            var refreshTokenCount =
                await factory.QueryScalarAsync<int>(
                    $"""
                 SELECT COUNT(1)
                 FROM [identity].RefreshTokens AS rt
                 INNER JOIN [identity].Users AS u
                     ON u.UserId = rt.UserId
                 WHERE u.Email = '{email}';
                 """);

            throw new Xunit.Sdk.XunitException(
                $"""
             Registration returned {(int)response.StatusCode}
             {response.StatusCode}.

             Response:
             {responseBody}

             Database diagnostic:
             UserCount = {userCount}
             ActiveUserCount = {activeUserCount}
             RoleCount = {roleCount}
             SecurityEventCount = {securityEventCount}
             RefreshTokenCount = {refreshTokenCount}
             """);
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<AuthenticationResponse>();

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
}
