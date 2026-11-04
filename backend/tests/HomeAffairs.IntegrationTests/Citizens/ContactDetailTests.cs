using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HomeAffairs.Contracts.Citizens;
using HomeAffairs.IntegrationTests.Identity;
using HomeAffairs.IntegrationTests.Infrastructure;

namespace HomeAffairs.IntegrationTests.Citizens;

[Collection(IntegrationTestCollection.Name)]
public sealed class ContactDetailTests(
    HomeAffairsApiFactory factory)
{
    [Fact]
    public async Task Citizen_should_create_and_read_contact_details()
    {
        var identity =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        await CreateProfileAsync(
            identity.AccessToken);

        var emailResponse =
            await CreateContactAsync(
                identity.AccessToken,
                new CreateContactDetailRequest(
                    "EMAIL",
                    IdentityTestHelper.UniqueEmail(
                        "contact"),
                    true));

        emailResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var mobileResponse =
            await CreateContactAsync(
                identity.AccessToken,
                new CreateContactDetailRequest(
                    "MOBILE",
                    "+27820000001",
                    true));

        mobileResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        using var getRequest =
            IdentityTestHelper
                .CreateAuthenticatedRequest(
                    HttpMethod.Get,
                    "/api/v1/citizens/me/contacts",
                    identity.AccessToken);

        var getResponse =
            await factory.Client.SendAsync(
                getRequest);

        getResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var contacts =
            await getResponse.Content
                .ReadFromJsonAsync<
                    ContactDetailResponse[]>();

        contacts.Should().NotBeNull();

        contacts!
            .Should()
            .HaveCount(2);

        contacts
            .Should()
            .Contain(contact =>
                contact.ContactType == "EMAIL"
                && contact.IsPrimary);

        contacts
            .Should()
            .Contain(contact =>
                contact.ContactType == "MOBILE"
                && contact.IsPrimary);
    }

    [Fact]
    public async Task Duplicate_contact_should_return_conflict()
    {
        var identity =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        await CreateProfileAsync(
            identity.AccessToken);

        var email =
            IdentityTestHelper.UniqueEmail(
                "duplicate-contact");

        var request =
            new CreateContactDetailRequest(
                "EMAIL",
                email,
                true);

        var first =
            await CreateContactAsync(
                identity.AccessToken,
                request);

        var second =
            await CreateContactAsync(
                identity.AccessToken,
                request);

        first.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        second.StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);
    }

    private async Task CreateProfileAsync(
        string accessToken)
    {
        var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/v1/citizens/me");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        request.Content =
            JsonContent.Create(
                new CreateCitizenProfileRequest(
                    null,
                    "Contact",
                    null,
                    "Citizen",
                    new DateOnly(
                        1990,
                        1,
                        1),
                    "F"));

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);
    }

    private async Task<HttpResponseMessage>
        CreateContactAsync(
            string accessToken,
            CreateContactDetailRequest contact)
    {
        var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/v1/citizens/me/contacts");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        request.Content =
            JsonContent.Create(contact);

        return await factory.Client
            .SendAsync(request);
    }
}
