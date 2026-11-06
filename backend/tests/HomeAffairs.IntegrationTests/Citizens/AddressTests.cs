using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HomeAffairs.Contracts.Citizens;
using HomeAffairs.IntegrationTests.Identity;
using HomeAffairs.IntegrationTests.Infrastructure;

namespace HomeAffairs.IntegrationTests.Citizens;

[Collection(IntegrationTestCollection.Name)]
public sealed class AddressTests(
    HomeAffairsApiFactory factory)
{
    [Fact]
    public async Task Citizen_should_create_and_read_address()
    {
        var identity =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        await CreateProfileAsync(
            identity.AccessToken);

        var createResponse =
            await CreateAddressAsync(
                identity.AccessToken,
                CreateResidentialAddress(
                    "10 Integration Street",
                    isPrimary: true));

        createResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var created =
            await createResponse.Content
                .ReadFromJsonAsync<AddressResponse>();

        created.Should().NotBeNull();

        created!.AddressType
            .Should()
            .Be("RESIDENTIAL");

        created.AddressLine1
            .Should()
            .Be("10 Integration Street");

        created.CountryCode
            .Should()
            .Be("ZA");

        created.IsPrimary
            .Should()
            .BeTrue();

        created.IsActive
            .Should()
            .BeTrue();

        created.RowVersion
            .Should()
            .NotBeNullOrWhiteSpace();

        var addresses =
            await GetAddressesAsync(
                identity.AccessToken);

        addresses
            .Should()
            .ContainSingle(
                address =>
                    address.AddressId ==
                    created.AddressId);

        var loaded =
            addresses.Single(
                address =>
                    address.AddressId ==
                    created.AddressId);

        loaded.AddressLine1
            .Should()
            .Be("10 Integration Street");
    }

    [Fact]
    public async Task First_address_of_type_should_be_primary()
    {
        var identity =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        await CreateProfileAsync(
            identity.AccessToken);

        /*
            Explicitly request false.

            The database rule should automatically make
            the first active address of each type primary.
        */
        var createResponse =
            await CreateAddressAsync(
                identity.AccessToken,
                CreateResidentialAddress(
                    "11 First Primary Street",
                    isPrimary: false));

        createResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var created =
            await createResponse.Content
                .ReadFromJsonAsync<AddressResponse>();

        created.Should().NotBeNull();

        created!.IsPrimary
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task Updating_address_to_primary_should_demote_previous_primary()
    {
        var identity =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        await CreateProfileAsync(
            identity.AccessToken);

        var firstResponse =
            await CreateAddressAsync(
                identity.AccessToken,
                CreateResidentialAddress(
                    "20 Primary Street",
                    isPrimary: true));

        firstResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var first =
            await firstResponse.Content
                .ReadFromJsonAsync<AddressResponse>();

        first.Should().NotBeNull();

        var secondResponse =
            await CreateAddressAsync(
                identity.AccessToken,
                CreateResidentialAddress(
                    "21 Secondary Street",
                    isPrimary: false));

        secondResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var second =
            await secondResponse.Content
                .ReadFromJsonAsync<AddressResponse>();

        second.Should().NotBeNull();

        first!.IsPrimary
            .Should()
            .BeTrue();

        second!.IsPrimary
            .Should()
            .BeFalse();

        var updateResponse =
            await UpdateAddressAsync(
                identity.AccessToken,
                second.AddressId,
                new UpdateAddressRequest(
                    "RESIDENTIAL",
                    second.AddressLine1,
                    second.AddressLine2,
                    second.Suburb,
                    second.CityOrTown,
                    second.Province,
                    second.PostalCode,
                    second.CountryCode,
                    true,
                    second.RowVersion));

        updateResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var updated =
            await updateResponse.Content
                .ReadFromJsonAsync<AddressResponse>();

        updated.Should().NotBeNull();

        updated!.IsPrimary
            .Should()
            .BeTrue();

        updated.RowVersion
            .Should()
            .NotBe(second.RowVersion);

        var addresses =
            await GetAddressesAsync(
                identity.AccessToken);

        var residential =
            addresses
                .Where(address =>
                    address.AddressType ==
                    "RESIDENTIAL")
                .ToArray();

        residential
            .Should()
            .HaveCount(2);

        residential
            .Count(address =>
                address.IsPrimary)
            .Should()
            .Be(1);

        residential
            .Single(address =>
                address.IsPrimary)
            .AddressId
            .Should()
            .Be(second.AddressId);

        residential
            .Single(address =>
                address.AddressId ==
                first.AddressId)
            .IsPrimary
            .Should()
            .BeFalse();
    }

    [Fact]
    public async Task Stale_address_update_should_return_conflict()
    {
        var identity =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        await CreateProfileAsync(
            identity.AccessToken);

        var createResponse =
            await CreateAddressAsync(
                identity.AccessToken,
                CreateResidentialAddress(
                    "30 Concurrency Street",
                    isPrimary: true));

        var created =
            await createResponse.Content
                .ReadFromJsonAsync<AddressResponse>();

        created.Should().NotBeNull();

        var staleRowVersion =
            created!.RowVersion;

        var firstUpdate =
            await UpdateAddressAsync(
                identity.AccessToken,
                created.AddressId,
                new UpdateAddressRequest(
                    "RESIDENTIAL",
                    "31 Updated Street",
                    null,
                    "Concurrency Suburb",
                    "Cape Town",
                    "Western Cape",
                    "8001",
                    "ZA",
                    true,
                    staleRowVersion));

        firstUpdate.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var successfulUpdate =
            await firstUpdate.Content
                .ReadFromJsonAsync<AddressResponse>();

        successfulUpdate.Should().NotBeNull();

        successfulUpdate!.RowVersion
            .Should()
            .NotBe(staleRowVersion);

        /*
            Simulate another client that still holds the
            original RowVersion.
        */
        var staleUpdate =
            await UpdateAddressAsync(
                identity.AccessToken,
                created.AddressId,
                new UpdateAddressRequest(
                    "RESIDENTIAL",
                    "32 Stale Street",
                    null,
                    "Stale Suburb",
                    "Cape Town",
                    "Western Cape",
                    "8001",
                    "ZA",
                    true,
                    staleRowVersion));

        staleUpdate.StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);

        /*
            Confirm the stale request did not overwrite
            the successful update.
        */
        var addresses =
            await GetAddressesAsync(
                identity.AccessToken);

        var persisted =
            addresses.Single(
                address =>
                    address.AddressId ==
                    created.AddressId);

        persisted.AddressLine1
            .Should()
            .Be("31 Updated Street");
    }

    [Fact]
    public async Task Deleting_primary_address_should_promote_remaining_address()
    {
        var identity =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        await CreateProfileAsync(
            identity.AccessToken);

        var primaryResponse =
            await CreateAddressAsync(
                identity.AccessToken,
                CreateResidentialAddress(
                    "40 Primary Delete Street",
                    isPrimary: true));

        var primary =
            await primaryResponse.Content
                .ReadFromJsonAsync<AddressResponse>();

        primary.Should().NotBeNull();

        var secondaryResponse =
            await CreateAddressAsync(
                identity.AccessToken,
                CreateResidentialAddress(
                    "41 Replacement Street",
                    isPrimary: false));

        var secondary =
            await secondaryResponse.Content
                .ReadFromJsonAsync<AddressResponse>();

        secondary.Should().NotBeNull();

        primary!.IsPrimary
            .Should()
            .BeTrue();

        secondary!.IsPrimary
            .Should()
            .BeFalse();

        var deleteResponse =
            await DeleteAddressAsync(
                identity.AccessToken,
                primary.AddressId,
                primary.RowVersion);

        deleteResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.NoContent);

        var addresses =
            await GetAddressesAsync(
                identity.AccessToken);

        addresses
            .Should()
            .NotContain(
                address =>
                    address.AddressId ==
                    primary.AddressId);

        var replacement =
            addresses.Single(
                address =>
                    address.AddressId ==
                    secondary.AddressId);

        replacement.IsPrimary
            .Should()
            .BeTrue();

        /*
            Verify the record was soft-deleted rather
            than physically removed.
        */
        var softDeletedCount =
            await factory.QueryScalarAsync<int>(
                $"""
                 SELECT COUNT(1)
                 FROM [citizens].Addresses
                 WHERE AddressId = '{primary.AddressId}'
                   AND IsActive = 0
                   AND IsPrimary = 0;
                 """);

        softDeletedCount
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task Other_citizen_should_not_modify_address()
    {
        var owner =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        await CreateProfileAsync(
            owner.AccessToken);

        var createResponse =
            await CreateAddressAsync(
                owner.AccessToken,
                CreateResidentialAddress(
                    "50 Owner Street",
                    isPrimary: true));

        var ownerAddress =
            await createResponse.Content
                .ReadFromJsonAsync<AddressResponse>();

        ownerAddress.Should().NotBeNull();

        var otherCitizen =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        await CreateProfileAsync(
            otherCitizen.AccessToken);

        var updateResponse =
            await UpdateAddressAsync(
                otherCitizen.AccessToken,
                ownerAddress!.AddressId,
                new UpdateAddressRequest(
                    "RESIDENTIAL",
                    "Unauthorized Change",
                    null,
                    "Invalid Suburb",
                    "Cape Town",
                    "Western Cape",
                    "8000",
                    "ZA",
                    true,
                    ownerAddress.RowVersion));

        /*
            404 is intentional.

            We do not reveal that another Citizen's
            AddressId exists.
        */
        updateResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);

        var ownerAddresses =
            await GetAddressesAsync(
                owner.AccessToken);

        var persisted =
            ownerAddresses.Single(
                address =>
                    address.AddressId ==
                    ownerAddress.AddressId);

        persisted.AddressLine1
            .Should()
            .Be("50 Owner Street");
    }

    [Fact]
    public async Task First_postal_and_residential_addresses_should_each_be_primary()
    {
        var identity =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        await CreateProfileAsync(
            identity.AccessToken);

        var residentialResponse =
            await CreateAddressAsync(
                identity.AccessToken,
                CreateResidentialAddress(
                    "60 Residential Street",
                    isPrimary: false));

        var postalResponse =
            await CreateAddressAsync(
                identity.AccessToken,
                new CreateAddressRequest(
                    "POSTAL",
                    "PO Box 600",
                    null,
                    null,
                    "Cape Town",
                    "Western Cape",
                    "8000",
                    "ZA",
                    false));

        residentialResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        postalResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var residential =
            await residentialResponse.Content
                .ReadFromJsonAsync<AddressResponse>();

        var postal =
            await postalResponse.Content
                .ReadFromJsonAsync<AddressResponse>();

        residential.Should().NotBeNull();
        postal.Should().NotBeNull();

        residential!.IsPrimary
            .Should()
            .BeTrue();

        postal!.IsPrimary
            .Should()
            .BeTrue();

        residential.AddressType
            .Should()
            .Be("RESIDENTIAL");

        postal.AddressType
            .Should()
            .Be("POSTAL");
    }

    [Fact]
    public async Task Invalid_row_version_should_return_bad_request()
    {
        var identity =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        await CreateProfileAsync(
            identity.AccessToken);

        var createResponse =
            await CreateAddressAsync(
                identity.AccessToken,
                CreateResidentialAddress(
                    "70 Validation Street",
                    isPrimary: true));

        var address =
            await createResponse.Content
                .ReadFromJsonAsync<AddressResponse>();

        address.Should().NotBeNull();

        var updateResponse =
            await UpdateAddressAsync(
                identity.AccessToken,
                address!.AddressId,
                new UpdateAddressRequest(
                    "RESIDENTIAL",
                    address.AddressLine1,
                    address.AddressLine2,
                    address.Suburb,
                    address.CityOrTown,
                    address.Province,
                    address.PostalCode,
                    address.CountryCode,
                    true,
                    "not-valid-base64!!!"));

        updateResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.BadRequest);
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
                    "Address",
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
        CreateAddressAsync(
            string accessToken,
            CreateAddressRequest address)
    {
        var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/v1/citizens/me/addresses");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        request.Content =
            JsonContent.Create(address);

        return await factory.Client.SendAsync(
            request);
    }

    private async Task<AddressResponse[]>
        GetAddressesAsync(
            string accessToken)
    {
        using var request =
            IdentityTestHelper
                .CreateAuthenticatedRequest(
                    HttpMethod.Get,
                    "/api/v1/citizens/me/addresses",
                    accessToken);

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var results =
            await response.Content
                .ReadFromJsonAsync<AddressResponse[]>();

        results.Should().NotBeNull();

        return results!;
    }

    private async Task<HttpResponseMessage>
        UpdateAddressAsync(
            string accessToken,
            Guid addressId,
            UpdateAddressRequest update)
    {
        var request =
            new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/v1/citizens/me/addresses/" +
                $"{addressId}");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        request.Content =
            JsonContent.Create(update);

        return await factory.Client.SendAsync(
            request);
    }

    private async Task<HttpResponseMessage>
        DeleteAddressAsync(
            string accessToken,
            Guid addressId,
            string rowVersion)
    {
        var request =
            new HttpRequestMessage(
                HttpMethod.Delete,
                $"/api/v1/citizens/me/addresses/" +
                $"{addressId}");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        request.Content =
            JsonContent.Create(
                new DeleteAddressRequest(
                    rowVersion));

        return await factory.Client.SendAsync(
            request);
    }

    private static CreateAddressRequest
        CreateResidentialAddress(
            string addressLine1,
            bool isPrimary)
    {
        return new CreateAddressRequest(
            "RESIDENTIAL",
            addressLine1,
            null,
            "Integration Suburb",
            "Cape Town",
            "Western Cape",
            "8001",
            "ZA",
            isPrimary);
    }
}
