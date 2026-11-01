using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HomeAffairs.Contracts.Identity;
using HomeAffairs.IntegrationTests.Infrastructure;

namespace HomeAffairs.IntegrationTests.Identity;

[Collection(IntegrationTestCollection.Name)]
public sealed class LoginTests(
    HomeAffairsApiFactory factory)
{
    [Fact]
    public async Task Incorrect_password_should_return_unauthorized()
    {
        var email =
            $"login-{Guid.NewGuid():N}@example.com";

        await factory.Client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(
                email,
                "HomeAffairs-Test-2026!",
                "Login",
                "Test"));

        var response =
            await factory.Client.PostAsJsonAsync(
                "/api/v1/auth/login",
                new LoginRequest(
                    email,
                    "Wrong-Password-2026!"));

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Unauthorized);
    }
}
