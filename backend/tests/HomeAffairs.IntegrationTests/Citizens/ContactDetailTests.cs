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

    [Fact]
    public async Task Updating_contact_to_primary_should_demote_previous_primary()
    {
        var identity =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        await CreateProfileAsync(
            identity.AccessToken);

        var firstResponse =
            await CreateContactAsync(
                identity.AccessToken,
                new CreateContactDetailRequest(
                    "EMAIL",
                    IdentityTestHelper.UniqueEmail(
                        "primary-one"),
                    true));

        var secondResponse =
            await CreateContactAsync(
                identity.AccessToken,
                new CreateContactDetailRequest(
                    "EMAIL",
                    IdentityTestHelper.UniqueEmail(
                        "primary-two"),
                    false));

        var first =
            await firstResponse.Content
                .ReadFromJsonAsync<
                    ContactDetailResponse>();

        var second =
            await secondResponse.Content
                .ReadFromJsonAsync<
                    ContactDetailResponse>();

        first.Should().NotBeNull();
        second.Should().NotBeNull();

        var updateRequest =
            new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/v1/citizens/me/contacts/" +
                $"{second!.ContactDetailId}");

        updateRequest.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                identity.AccessToken);

        updateRequest.Content =
            JsonContent.Create(
                new UpdateContactDetailRequest(
                    "EMAIL",
                    second.ContactValue,
                    true,
                    second.RowVersion));

        var updateResponse =
            await factory.Client.SendAsync(
                updateRequest);

        updateResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        using var getRequest =
            IdentityTestHelper
                .CreateAuthenticatedRequest(
                    HttpMethod.Get,
                    "/api/v1/citizens/me/contacts",
                    identity.AccessToken);

        var getResponse =
            await factory.Client.SendAsync(
                getRequest);

        var contacts =
            await getResponse.Content
                .ReadFromJsonAsync<
                    ContactDetailResponse[]>();

        contacts.Should().NotBeNull();

        var emailContacts =
            contacts!
                .Where(contact =>
                    contact.ContactType == "EMAIL")
                .ToArray();

        emailContacts
            .Count(contact => contact.IsPrimary)
            .Should()
            .Be(1);

        emailContacts
            .Single(contact => contact.IsPrimary)
            .ContactDetailId
            .Should()
            .Be(second.ContactDetailId);
    }

    [Fact]
    public async Task Stale_contact_update_should_return_conflict()
    {
        var identity =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        await CreateProfileAsync(
            identity.AccessToken);

        var createResponse =
            await CreateContactAsync(
                identity.AccessToken,
                new CreateContactDetailRequest(
                    "EMAIL",
                    IdentityTestHelper.UniqueEmail(
                        "concurrency"),
                    true));

        var contact =
            await createResponse.Content
                .ReadFromJsonAsync<
                    ContactDetailResponse>();

        contact.Should().NotBeNull();

        async Task<HttpResponseMessage> UpdateAsync(
            string value)
        {
            var request =
                new HttpRequestMessage(
                    HttpMethod.Put,
                    $"/api/v1/citizens/me/contacts/" +
                    $"{contact!.ContactDetailId}");

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    identity.AccessToken);

            request.Content =
                JsonContent.Create(
                    new UpdateContactDetailRequest(
                        "EMAIL",
                        value,
                        true,
                        contact.RowVersion));

            return await factory.Client
                .SendAsync(request);
        }

        var first =
            await UpdateAsync(
                IdentityTestHelper.UniqueEmail(
                    "fresh"));

        first.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var stale =
            await UpdateAsync(
                IdentityTestHelper.UniqueEmail(
                    "stale"));

        stale.StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Deleting_primary_contact_should_promote_remaining_contact()
    {
        var identity =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        await CreateProfileAsync(
            identity.AccessToken);

        var primaryResponse =
            await CreateContactAsync(
                identity.AccessToken,
                new CreateContactDetailRequest(
                    "MOBILE",
                    "+27821110001",
                    true));

        var secondaryResponse =
            await CreateContactAsync(
                identity.AccessToken,
                new CreateContactDetailRequest(
                    "MOBILE",
                    "+27821110002",
                    false));

        var primary =
            await primaryResponse.Content
                .ReadFromJsonAsync<
                    ContactDetailResponse>();

        var secondary =
            await secondaryResponse.Content
                .ReadFromJsonAsync<
                    ContactDetailResponse>();

        primary.Should().NotBeNull();
        secondary.Should().NotBeNull();

        var deleteRequest =
            new HttpRequestMessage(
                HttpMethod.Delete,
                $"/api/v1/citizens/me/contacts/" +
                $"{primary!.ContactDetailId}");

        deleteRequest.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                identity.AccessToken);

        deleteRequest.Content =
            JsonContent.Create(
                new DeleteContactDetailRequest(
                    primary.RowVersion));

        var deleteResponse =
            await factory.Client.SendAsync(
                deleteRequest);

        deleteResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.NoContent);

        using var getRequest =
            IdentityTestHelper
                .CreateAuthenticatedRequest(
                    HttpMethod.Get,
                    "/api/v1/citizens/me/contacts",
                    identity.AccessToken);

        var getResponse =
            await factory.Client.SendAsync(
                getRequest);

        var contacts =
            await getResponse.Content
                .ReadFromJsonAsync<
                    ContactDetailResponse[]>();

        contacts.Should().NotBeNull();

        contacts!
            .Should()
            .NotContain(contact =>
                contact.ContactDetailId ==
                primary.ContactDetailId);

        var remaining =
            contacts.Single(contact =>
                contact.ContactDetailId ==
                secondary!.ContactDetailId);

        remaining.IsPrimary
            .Should()
            .BeTrue();
    }
}
