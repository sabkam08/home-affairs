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
                $"Registration returned " +
                $"{(int)response.StatusCode} " +
                $"{response.StatusCode}.{Environment.NewLine}" +
                $"Response:{Environment.NewLine}{error}");
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    AuthenticationResponse>();

        result.Should().NotBeNull();

        result!.Email.Should().Be(email);

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
