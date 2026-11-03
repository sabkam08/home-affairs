using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HomeAffairs.Contracts.Identity;
using HomeAffairs.IntegrationTests.Infrastructure;

namespace HomeAffairs.IntegrationTests.Identity;

internal static class IdentityTestHelper
{
    public const string ValidPassword =
        "HomeAffairs-Test-2026!";

    public static string UniqueEmail(
        string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}@example.com";
    }

    public static async Task<TestAuthenticationResponse>
        RegisterCitizenAsync(
            HomeAffairsApiFactory factory,
            string? email = null)
    {
        email ??= UniqueEmail("citizen");

        var response =
            await factory.Client.PostAsJsonAsync(
                "/api/v1/auth/register",
                new RegisterRequest(
                    email,
                    ValidPassword,
                    "Integration",
                    "Citizen"));

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<
                TestAuthenticationResponse>();

        result.Should().NotBeNull();

        return result!;
    }

    public static async Task<TestAuthenticationResponse>
        LoginAsync(
            HomeAffairsApiFactory factory,
            string email,
            string password = ValidPassword)
    {
        var response =
            await factory.Client.PostAsJsonAsync(
                "/api/v1/auth/login",
                new LoginRequest(
                    email,
                    password));

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<
                TestAuthenticationResponse>();

        result.Should().NotBeNull();

        return result!;
    }

    public static HttpRequestMessage
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

    public static async Task AssignRoleDirectlyAsync(
        HomeAffairsApiFactory factory,
        Guid userId,
        string roleCode)
    {
        var safeRoleCode =
            roleCode.Replace(
                "'",
                "''",
                StringComparison.Ordinal);

        await factory.ExecuteSqlAsync(
            $"""
             DECLARE @RoleId UNIQUEIDENTIFIER;

             SELECT
                 @RoleId = RoleId
             FROM [identity].Roles
             WHERE Code = N'{safeRoleCode}';

             IF @RoleId IS NULL
             BEGIN
                 THROW 51901,
                     'Integration-test role was not found.',
                     1;
             END;

             IF NOT EXISTS
             (
                 SELECT 1
                 FROM [identity].UserRoles
                 WHERE UserId = '{userId}'
                   AND RoleId = @RoleId
             )
             BEGIN
                 INSERT INTO [identity].UserRoles
                 (
                     UserId,
                     RoleId,
                     AssignedAtUtc,
                     AssignedByUserId
                 )
                 VALUES
                 (
                     '{userId}',
                     @RoleId,
                     SYSUTCDATETIME(),
                     '{userId}'
                 );
             END;
             """);
    }
}
