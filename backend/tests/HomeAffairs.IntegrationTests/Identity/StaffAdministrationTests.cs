using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HomeAffairs.Contracts.Identity;
using HomeAffairs.IntegrationTests.Infrastructure;
using HomeAffairs.Application.Identity.Administration;

namespace HomeAffairs.IntegrationTests.Identity;

[Collection(IntegrationTestCollection.Name)]
public sealed class StaffAdministrationTests(
    HomeAffairsApiFactory factory)
{
    [Fact]
    public async Task Security_administrator_should_create_practitioner()
    {
        var administrator =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        await IdentityTestHelper
            .AssignRoleDirectlyAsync(
                factory,
                administrator.UserId,
                "SECURITY_ADMINISTRATOR");

        /*
            Login again so the response/JWT roles reflect the
            role introduced during test setup.
        */
        var administratorLogin =
            await IdentityTestHelper.LoginAsync(
                factory,
                administrator.Email);

        var staffEmail =
            IdentityTestHelper.UniqueEmail(
                "practitioner");

        var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/v1/identity/users/staff");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                administratorLogin.AccessToken);

        request.Content =
            JsonContent.Create(
                new CreateStaffUserRequest(
                    staffEmail,
                    "Practitioner-Test-2026!",
                    "Integration",
                    "Practitioner",
                    "PRACTITIONER"));

        var response =
            await factory.Client.SendAsync(request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var result =
            await response.Content.ReadFromJsonAsync<
                CreateStaffUserResult>();

        result.Should().NotBeNull();

        result!.Email.Should().Be(staffEmail);

        result.Roles
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .Be("PRACTITIONER");

        var practitionerLogin =
            await IdentityTestHelper.LoginAsync(
                factory,
                staffEmail,
                "Practitioner-Test-2026!");

        using var practitionerRequest =
            IdentityTestHelper
                .CreateAuthenticatedRequest(
                    HttpMethod.Get,
                    "/api/v1/authorization-test/practitioner",
                    practitionerLogin.AccessToken);

        var practitionerResponse =
            await factory.Client.SendAsync(
                practitionerRequest);

        practitionerResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Citizen_should_not_create_staff_user()
    {
        var citizen =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/v1/identity/users/staff");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                citizen.AccessToken);

        request.Content =
            JsonContent.Create(
                new CreateStaffUserRequest(
                    IdentityTestHelper.UniqueEmail(
                        "forbidden-staff"),
                    "Practitioner-Test-2026!",
                    "Forbidden",
                    "Staff",
                    "PRACTITIONER"));

        var response =
            await factory.Client.SendAsync(request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Forbidden);
    }
}
