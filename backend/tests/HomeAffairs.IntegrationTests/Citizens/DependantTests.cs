using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HomeAffairs.Contracts.Citizens;
using HomeAffairs.IntegrationTests.Identity;
using HomeAffairs.IntegrationTests.Infrastructure;

namespace HomeAffairs.IntegrationTests.Citizens;

[Collection(IntegrationTestCollection.Name)]
public sealed class DependantTests(
    HomeAffairsApiFactory factory)
{
    [Fact]
    public async Task Adult_should_create_represented_minor()
    {
        var adult =
            await CreateAdultCitizenAsync();

        var response =
            await CreateDependantAsync(
                adult.AccessToken,
                CreateMinorRequest(
                    "BIOLOGICAL_PARENT"));

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var dependant =
            await response.Content
                .ReadFromJsonAsync<DependantResponse>();

        dependant.Should().NotBeNull();

        dependant!.RelationshipType
            .Should()
            .Be("BIOLOGICAL_PARENT");

        dependant.FirstName
            .Should()
            .Be("Child");

        dependant.LastName
            .Should()
            .Be("Citizen");

        dependant.IsMinor
            .Should()
            .BeTrue();

        dependant.IsActive
            .Should()
            .BeTrue();

        dependant.SubjectCitizenId
            .Should()
            .NotBe(Guid.Empty);

        dependant.CitizenRelationshipId
            .Should()
            .NotBe(Guid.Empty);

        dependant.RelationshipRowVersion
            .Should()
            .NotBeNullOrWhiteSpace();

        dependant.SubjectRowVersion
            .Should()
            .NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Represented_minor_should_not_have_identity_user()
    {
        var adult =
            await CreateAdultCitizenAsync();

        var response =
            await CreateDependantAsync(
                adult.AccessToken,
                CreateMinorRequest(
                    "LEGAL_GUARDIAN"));

        response.EnsureSuccessStatusCode();

        var dependant =
            await response.Content
                .ReadFromJsonAsync<DependantResponse>();

        dependant.Should().NotBeNull();

        var citizenUserCount =
            await factory.QueryScalarAsync<int>(
                $"""
                 SELECT COUNT(1)
                 FROM [citizens].Citizens
                 WHERE CitizenId =
                     '{dependant!.SubjectCitizenId}'
                   AND UserId IS NULL;
                 """);

        citizenUserCount
            .Should()
            .Be(1);

        /*
            Also verify there is no Identity account linked
            to the represented subject.
        */
        var linkedIdentityCount =
            await factory.QueryScalarAsync<int>(
                $"""
                 SELECT COUNT(1)
                 FROM [identity].Users AS u
                 INNER JOIN [citizens].Citizens AS c
                     ON c.UserId = u.UserId
                 WHERE c.CitizenId =
                     '{dependant.SubjectCitizenId}';
                 """);

        linkedIdentityCount
            .Should()
            .Be(0);
    }

    [Fact]
    public async Task Adult_should_read_own_dependants()
    {
        var adult =
            await CreateAdultCitizenAsync();

        var firstResponse =
            await CreateDependantAsync(
                adult.AccessToken,
                CreateMinorRequest(
                    "BIOLOGICAL_PARENT",
                    firstName: "First"));

        var secondResponse =
            await CreateDependantAsync(
                adult.AccessToken,
                CreateMinorRequest(
                    "LEGAL_GUARDIAN",
                    firstName: "Second"));

        firstResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        secondResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        using var request =
            IdentityTestHelper
                .CreateAuthenticatedRequest(
                    HttpMethod.Get,
                    "/api/v1/citizens/me/dependants",
                    adult.AccessToken);

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var dependants =
            await response.Content
                .ReadFromJsonAsync<
                    DependantResponse[]>();

        dependants.Should().NotBeNull();

        dependants!
            .Should()
            .HaveCount(2);

        dependants
            .Should()
            .Contain(dependant =>
                dependant.FirstName == "First");

        dependants
            .Should()
            .Contain(dependant =>
                dependant.FirstName == "Second");
    }

    [Fact]
    public async Task Adult_should_read_single_owned_dependant()
    {
        var adult =
            await CreateAdultCitizenAsync();

        var createResponse =
            await CreateDependantAsync(
                adult.AccessToken,
                CreateMinorRequest(
                    "ADOPTIVE_PARENT"));

        createResponse.EnsureSuccessStatusCode();

        var created =
            await createResponse.Content
                .ReadFromJsonAsync<
                    DependantResponse>();

        created.Should().NotBeNull();

        using var request =
            IdentityTestHelper
                .CreateAuthenticatedRequest(
                    HttpMethod.Get,
                    $"/api/v1/citizens/me/dependants/" +
                    $"{created!.SubjectCitizenId}",
                    adult.AccessToken);

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var loaded =
            await response.Content
                .ReadFromJsonAsync<
                    DependantResponse>();

        loaded.Should().NotBeNull();

        loaded!.SubjectCitizenId
            .Should()
            .Be(created.SubjectCitizenId);

        loaded.CitizenRelationshipId
            .Should()
            .Be(created.CitizenRelationshipId);

        loaded.RelationshipType
            .Should()
            .Be("ADOPTIVE_PARENT");
    }

    [Fact]
    public async Task Adult_subject_should_be_rejected()
    {
        var adult =
            await CreateAdultCitizenAsync();

        var request =
            new CreateDependantRequest(
                null,
                "Adult",
                null,
                "Subject",
                new DateOnly(
                    1990,
                    1,
                    1),
                "M",
                "BIOLOGICAL_PARENT");

        var response =
            await CreateDependantAsync(
                adult.AccessToken,
                request);

        /*
            FluentValidation rejects the request before
            persistence because the represented subject
            is not under 18.
        */
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.BadRequest);

        var citizenCount =
            await factory.QueryScalarAsync<int>(
                """
                SELECT COUNT(1)
                FROM [citizens].Citizens
                WHERE FirstName = 'Adult'
                  AND LastName = 'Subject'
                  AND UserId IS NULL;
                """);

        citizenCount
            .Should()
            .Be(0);
    }

    [Fact]
    public async Task Minor_representative_should_be_rejected()
    {
        /*
            Register an Identity account and deliberately
            create a Citizen profile whose age is under 18.
        */
        var identity =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        await CreateProfileAsync(
            identity.AccessToken,
            new DateOnly(
                2012,
                1,
                1));

        var response =
            await CreateDependantAsync(
                identity.AccessToken,
                new CreateDependantRequest(
                    null,
                    "Younger",
                    null,
                    "Child",
                    new DateOnly(
                        2018,
                        1,
                        1),
                    "F",
                    "BIOLOGICAL_PARENT"));

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Other_citizen_should_not_read_dependant()
    {
        var owner =
            await CreateAdultCitizenAsync();

        var createResponse =
            await CreateDependantAsync(
                owner.AccessToken,
                CreateMinorRequest(
                    "LEGAL_GUARDIAN"));

        createResponse.EnsureSuccessStatusCode();

        var dependant =
            await createResponse.Content
                .ReadFromJsonAsync<
                    DependantResponse>();

        dependant.Should().NotBeNull();

        var otherAdult =
            await CreateAdultCitizenAsync();

        using var request =
            IdentityTestHelper
                .CreateAuthenticatedRequest(
                    HttpMethod.Get,
                    $"/api/v1/citizens/me/dependants/" +
                    $"{dependant!.SubjectCitizenId}",
                    otherAdult.AccessToken);

        var response =
            await factory.Client.SendAsync(
                request);

        /*
            404 is deliberate information hiding.

            The second Citizen must not be told that the
            represented Citizen exists for another adult.
        */
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("BIOLOGICAL_PARENT")]
    [InlineData("ADOPTIVE_PARENT")]
    [InlineData("LEGAL_GUARDIAN")]
    public async Task Supported_relationship_types_should_be_accepted(
        string relationshipType)
    {
        var adult =
            await CreateAdultCitizenAsync();

        var response =
            await CreateDependantAsync(
                adult.AccessToken,
                CreateMinorRequest(
                    relationshipType));

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var dependant =
            await response.Content
                .ReadFromJsonAsync<
                    DependantResponse>();

        dependant.Should().NotBeNull();

        dependant!.RelationshipType
            .Should()
            .Be(relationshipType);
    }

    [Fact]
    public async Task Invalid_relationship_type_should_be_rejected()
    {
        var adult =
            await CreateAdultCitizenAsync();

        var response =
            await CreateDependantAsync(
                adult.AccessToken,
                CreateMinorRequest(
                    "SIBLING"));

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.BadRequest);
    }

    private async Task<TestAuthenticationResponse>
        CreateAdultCitizenAsync()
    {
        var identity =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        await CreateProfileAsync(
            identity.AccessToken,
            new DateOnly(
                1990,
                1,
                1));

        return identity;
    }

    private async Task CreateProfileAsync(
        string accessToken,
        DateOnly dateOfBirth)
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
                    "Representative",
                    null,
                    "Citizen",
                    dateOfBirth,
                    "F"));

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);
    }

    private async Task<HttpResponseMessage>
        CreateDependantAsync(
            string accessToken,
            CreateDependantRequest dependant)
    {
        var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/v1/citizens/me/dependants");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        request.Content =
            JsonContent.Create(dependant);

        return await factory.Client.SendAsync(
            request);
    }

    private static CreateDependantRequest
        CreateMinorRequest(
            string relationshipType,
            string firstName = "Child")
    {
        return new CreateDependantRequest(
            null,
            firstName,
            "Integration",
            "Citizen",
            new DateOnly(
                2015,
                6,
                10),
            "F",
            relationshipType);
    }

    [Fact]
    public async Task Adult_should_update_relationship_type()
    {
        var adult =
            await CreateAdultCitizenAsync();

        var createResponse =
            await CreateDependantAsync(
                adult.AccessToken,
                CreateMinorRequest(
                    "BIOLOGICAL_PARENT"));

        var dependant =
            await createResponse.Content
                .ReadFromJsonAsync<DependantResponse>();

        dependant.Should().NotBeNull();

        var updateResponse =
            await UpdateRelationshipAsync(
                adult.AccessToken,
                dependant!.SubjectCitizenId,
                new UpdateDependantRelationshipRequest(
                    "LEGAL_GUARDIAN",
                    dependant.RelationshipRowVersion));

        updateResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var updated =
            await updateResponse.Content
                .ReadFromJsonAsync<DependantResponse>();

        updated.Should().NotBeNull();

        updated!.RelationshipType
            .Should()
            .Be("LEGAL_GUARDIAN");

        updated.RelationshipRowVersion
            .Should()
            .NotBe(
                dependant.RelationshipRowVersion);
    }

    [Fact]
    public async Task Stale_relationship_update_should_return_conflict()
    {
        var adult =
            await CreateAdultCitizenAsync();

        var createResponse =
            await CreateDependantAsync(
                adult.AccessToken,
                CreateMinorRequest(
                    "BIOLOGICAL_PARENT"));

        var dependant =
            await createResponse.Content
                .ReadFromJsonAsync<DependantResponse>();

        dependant.Should().NotBeNull();

        var staleRowVersion =
            dependant!.RelationshipRowVersion;

        var firstUpdate =
            await UpdateRelationshipAsync(
                adult.AccessToken,
                dependant.SubjectCitizenId,
                new UpdateDependantRelationshipRequest(
                    "LEGAL_GUARDIAN",
                    staleRowVersion));

        firstUpdate.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var staleUpdate =
            await UpdateRelationshipAsync(
                adult.AccessToken,
                dependant.SubjectCitizenId,
                new UpdateDependantRelationshipRequest(
                    "ADOPTIVE_PARENT",
                    staleRowVersion));

        staleUpdate.StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Adult_should_deactivate_relationship_without_deleting_subject()
    {
        var adult =
            await CreateAdultCitizenAsync();

        var createResponse =
            await CreateDependantAsync(
                adult.AccessToken,
                CreateMinorRequest(
                    "LEGAL_GUARDIAN"));

        var dependant =
            await createResponse.Content
                .ReadFromJsonAsync<DependantResponse>();

        dependant.Should().NotBeNull();

        var deleteResponse =
            await DeactivateRelationshipAsync(
                adult.AccessToken,
                dependant!.SubjectCitizenId,
                dependant.RelationshipRowVersion);

        deleteResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.NoContent);

        using var getRequest =
            IdentityTestHelper
                .CreateAuthenticatedRequest(
                    HttpMethod.Get,
                    $"/api/v1/citizens/me/dependants/" +
                    $"{dependant.SubjectCitizenId}",
                    adult.AccessToken);

        var getResponse =
            await factory.Client.SendAsync(
                getRequest);

        getResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);

        var inactiveRelationshipCount =
            await factory.QueryScalarAsync<int>(
                $"""
             SELECT COUNT(1)
             FROM [citizens].CitizenRelationships
             WHERE SubjectCitizenId =
                 '{dependant.SubjectCitizenId}'
               AND IsActive = 0;
             """);

        inactiveRelationshipCount
            .Should()
            .Be(1);

        var activeSubjectCount =
            await factory.QueryScalarAsync<int>(
                $"""
             SELECT COUNT(1)
             FROM [citizens].Citizens
             WHERE CitizenId =
                 '{dependant.SubjectCitizenId}'
               AND IsActive = 1
               AND UserId IS NULL;
             """);

        activeSubjectCount
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task Other_citizen_should_not_update_relationship()
    {
        var owner =
            await CreateAdultCitizenAsync();

        var createResponse =
            await CreateDependantAsync(
                owner.AccessToken,
                CreateMinorRequest(
                    "BIOLOGICAL_PARENT"));

        var dependant =
            await createResponse.Content
                .ReadFromJsonAsync<DependantResponse>();

        dependant.Should().NotBeNull();

        var otherAdult =
            await CreateAdultCitizenAsync();

        var response =
            await UpdateRelationshipAsync(
                otherAdult.AccessToken,
                dependant!.SubjectCitizenId,
                new UpdateDependantRelationshipRequest(
                    "LEGAL_GUARDIAN",
                    dependant.RelationshipRowVersion));

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Invalid_relationship_row_version_should_return_bad_request()
    {
        var adult =
            await CreateAdultCitizenAsync();

        var createResponse =
            await CreateDependantAsync(
                adult.AccessToken,
                CreateMinorRequest(
                    "BIOLOGICAL_PARENT"));

        var dependant =
            await createResponse.Content
                .ReadFromJsonAsync<DependantResponse>();

        dependant.Should().NotBeNull();

        var response =
            await UpdateRelationshipAsync(
                adult.AccessToken,
                dependant!.SubjectCitizenId,
                new UpdateDependantRelationshipRequest(
                    "LEGAL_GUARDIAN",
                    "not-valid-base64!!!"));

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.BadRequest);
    }

    private async Task<HttpResponseMessage>
    UpdateRelationshipAsync(
        string accessToken,
        Guid subjectCitizenId,
        UpdateDependantRelationshipRequest update)
    {
        var request =
            new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/v1/citizens/me/dependants/" +
                $"{subjectCitizenId}/relationship");

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
        DeactivateRelationshipAsync(
            string accessToken,
            Guid subjectCitizenId,
            string rowVersion)
    {
        var request =
            new HttpRequestMessage(
                HttpMethod.Delete,
                $"/api/v1/citizens/me/dependants/" +
                $"{subjectCitizenId}/relationship");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        request.Content =
            JsonContent.Create(
                new DeleteDependantRelationshipRequest(
                    rowVersion));

        return await factory.Client.SendAsync(
            request);
    }
}
