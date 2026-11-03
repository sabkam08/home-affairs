using System.Net;
using FluentAssertions;
using HomeAffairs.IntegrationTests.Infrastructure;

namespace HomeAffairs.IntegrationTests.Identity;

[Collection(IntegrationTestCollection.Name)]
public sealed class PermissionAuthorizationTests(
    HomeAffairsApiFactory factory)
{
    [Fact]
    public async Task Anonymous_request_should_return_unauthorized()
    {
        var response =
            await factory.Client.GetAsync(
                "/api/v1/authorization-test/citizen");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Citizen_should_access_create_permission()
    {
        var citizen =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        using var request =
            IdentityTestHelper
                .CreateAuthenticatedRequest(
                    HttpMethod.Get,
                    "/api/v1/authorization-test/citizen",
                    citizen.AccessToken);

        var response =
            await factory.Client.SendAsync(request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Citizen_should_not_access_practitioner_permission()
    {
        var citizen =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        using var request =
            IdentityTestHelper
                .CreateAuthenticatedRequest(
                    HttpMethod.Get,
                    "/api/v1/authorization-test/practitioner",
                    citizen.AccessToken);

        var response =
            await factory.Client.SendAsync(request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Permission_removal_should_affect_existing_token()
    {
        var citizen =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        using (var initialRequest =
               IdentityTestHelper
                   .CreateAuthenticatedRequest(
                       HttpMethod.Get,
                       "/api/v1/authorization-test/citizen",
                       citizen.AccessToken))
        {
            var initialResponse =
                await factory.Client.SendAsync(
                    initialRequest);

            initialResponse.StatusCode
                .Should()
                .Be(HttpStatusCode.OK);
        }

        await factory.ExecuteSqlAsync(
            """
            DELETE rp
            FROM [identity].RolePermissions AS rp
            INNER JOIN [identity].Roles AS r
                ON r.RoleId = rp.RoleId
            INNER JOIN [identity].Permissions AS p
                ON p.PermissionId = rp.PermissionId
            WHERE r.Code = 'CITIZEN'
              AND p.Code = 'APPLICATION.CREATE';
            """);

        try
        {
            using var deniedRequest =
                IdentityTestHelper
                    .CreateAuthenticatedRequest(
                        HttpMethod.Get,
                        "/api/v1/authorization-test/citizen",
                        citizen.AccessToken);

            var deniedResponse =
                await factory.Client.SendAsync(
                    deniedRequest);

            deniedResponse.StatusCode
                .Should()
                .Be(HttpStatusCode.Forbidden);
        }
        finally
        {
            await factory.ExecuteSqlAsync(
                """
                INSERT INTO [identity].RolePermissions
                (
                    RoleId,
                    PermissionId,
                    AssignedAtUtc
                )
                SELECT
                    r.RoleId,
                    p.PermissionId,
                    SYSUTCDATETIME()
                FROM [identity].Roles AS r
                CROSS JOIN [identity].Permissions AS p
                WHERE r.Code = 'CITIZEN'
                  AND p.Code = 'APPLICATION.CREATE'
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM [identity].RolePermissions AS rp
                      WHERE rp.RoleId = r.RoleId
                        AND rp.PermissionId =
                            p.PermissionId
                  );
                """);
        }

        using var restoredRequest =
            IdentityTestHelper
                .CreateAuthenticatedRequest(
                    HttpMethod.Get,
                    "/api/v1/authorization-test/citizen",
                    citizen.AccessToken);

        var restoredResponse =
            await factory.Client.SendAsync(
                restoredRequest);

        restoredResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);
    }
}
