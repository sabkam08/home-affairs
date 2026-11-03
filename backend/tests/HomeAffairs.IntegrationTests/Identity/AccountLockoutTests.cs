using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HomeAffairs.Contracts.Identity;
using HomeAffairs.IntegrationTests.Infrastructure;

namespace HomeAffairs.IntegrationTests.Identity;

[Collection(IntegrationTestCollection.Name)]
public sealed class AccountLockoutTests(
    HomeAffairsApiFactory factory)
{
    [Fact]
    public async Task Five_failed_logins_should_lock_account()
    {
        var email =
            IdentityTestHelper.UniqueEmail(
                "lockout");

        await IdentityTestHelper.RegisterCitizenAsync(
            factory,
            email);

        for (var attempt = 1;
             attempt <= 5;
             attempt++)
        {
            var response =
                await factory.Client.PostAsJsonAsync(
                    "/api/v1/auth/login",
                    new LoginRequest(
                        email,
                        "Incorrect-Password-2026!"));

            response.StatusCode
                .Should()
                .Be(HttpStatusCode.Unauthorized);
        }

        var lockedCount =
            await factory.QueryScalarAsync<int>(
                $"""
                 SELECT COUNT(1)
                 FROM [identity].Users
                 WHERE Email = '{email}'
                   AND FailedLoginAttempts = 0
                   AND LockedUntilUtc > SYSUTCDATETIME();
                 """);

                lockedCount
                    .Should()
                    .Be(
                        1,
                        "the fifth failed login should lock the account");

        var correctLogin =
            await factory.Client.PostAsJsonAsync(
                "/api/v1/auth/login",
                new LoginRequest(
                    email,
                    IdentityTestHelper.ValidPassword));

        correctLogin.StatusCode
            .Should()
            .Be(HttpStatusCode.Unauthorized);

        var lockedCount =
            await factory.QueryScalarAsync<int>(
                $"""
                 SELECT COUNT(1)
                 FROM [identity].Users
                 WHERE Email = '{email}'
                   AND LockedUntilUtc > SYSUTCDATETIME();
                 """);

        lockedCount.Should().Be(1);

        var eventCount =
            await factory.QueryScalarAsync<int>(
                $"""
                 SELECT COUNT(1)
                 FROM [identity].SecurityEvents AS se
                 INNER JOIN [identity].Users AS u
                     ON u.UserId = se.UserId
                 WHERE u.Email = '{email}'
                   AND se.EventType = 'ACCOUNT_LOCKED';
                 """);

        eventCount.Should().BeGreaterThanOrEqualTo(1);
    }
}
