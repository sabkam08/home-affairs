using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HomeAffairs.Contracts.Applications;
using HomeAffairs.Contracts.Citizens;
using HomeAffairs.Contracts.Documents;
using HomeAffairs.IntegrationTests.Identity;
using HomeAffairs.IntegrationTests.Infrastructure;

namespace HomeAffairs.IntegrationTests.Documents;

[Collection(IntegrationTestCollection.Name)]
public sealed class DocumentRequirementTests(
    HomeAffairsApiFactory factory)
{
    [Fact]
    public async Task Stolen_smart_id_should_have_three_required_document_types()
    {
        var citizen =
            await CreateCitizenAsync();

        using var request =
            Authenticated(
                HttpMethod.Get,
                "/api/v1/document-requirements/" +
                "SMART_ID_REPLACEMENT_STOLEN",
                citizen.AccessToken);

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var requirements =
            await response.Content
                .ReadFromJsonAsync<
                    DocumentRequirementResponse[]>();

        requirements.Should().NotBeNull();

        requirements!
            .Select(item =>
                item.DocumentType)
            .Should()
            .BeEquivalentTo(
                "AFFIDAVIT",
                "IDENTITY_DOCUMENT",
                "PROOF_OF_ADDRESS");
    }

    [Fact]
    public async Task New_application_should_not_be_ready_without_required_documents()
    {
        var setup =
            await CreateApplicationAsync();

        var readiness =
            await GetReadinessAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId);

        readiness.IsReady
            .Should()
            .BeFalse();

        readiness.RequiredCount
            .Should()
            .Be(3);

        readiness.SatisfiedCount
            .Should()
            .Be(0);

        readiness.Requirements
            .Should()
            .OnlyContain(item =>
                !item.IsSatisfied);
    }

    [Fact]
    public async Task Uploaded_required_document_should_increment_satisfied_count()
    {
        var setup =
            await CreateApplicationAsync();

        await UploadAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            "AFFIDAVIT");

        var readiness =
            await GetReadinessAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId);

        readiness.SatisfiedCount
            .Should()
            .Be(1);

        readiness.Requirements
            .Single(item =>
                item.DocumentType ==
                "AFFIDAVIT")
            .IsSatisfied
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task Application_should_be_ready_when_all_required_document_types_are_active()
    {
        var setup =
            await CreateApplicationAsync();

        await UploadAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            "AFFIDAVIT");

        await UploadAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            "IDENTITY_DOCUMENT");

        await UploadAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            "PROOF_OF_ADDRESS");

        var readiness =
            await GetReadinessAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId);

        readiness.IsReady
            .Should()
            .BeTrue();

        readiness.RequiredCount
            .Should()
            .Be(3);

        readiness.SatisfiedCount
            .Should()
            .Be(3);

        readiness.Requirements
            .Should()
            .OnlyContain(item =>
                item.IsSatisfied);
    }

    [Fact]
    public async Task Non_required_document_should_not_satisfy_missing_requirement()
    {
        var setup =
            await CreateApplicationAsync();

        await UploadAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            "SUPPORTING_DOCUMENT");

        var readiness =
            await GetReadinessAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId);

        readiness.SatisfiedCount
            .Should()
            .Be(0);

        readiness.IsReady
            .Should()
            .BeFalse();
    }

    [Fact]
    public async Task Other_citizen_should_not_read_application_readiness()
    {
        var owner =
            await CreateApplicationAsync();

        var other =
            await CreateCitizenAsync();

        using var request =
            Authenticated(
                HttpMethod.Get,
                $"/api/v1/applications/" +
                $"{owner.Application.ApplicationId}/" +
                "document-readiness",
                other.AccessToken);

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Invalid_application_type_requirements_should_return_bad_request()
    {
        var citizen =
            await CreateCitizenAsync();

        using var request =
            Authenticated(
                HttpMethod.Get,
                "/api/v1/document-requirements/" +
                "UNKNOWN_SERVICE",
                citizen.AccessToken);

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.BadRequest);
    }

    private sealed record Setup(
        TestAuthenticationResponse Identity,
        ApplicationResponse Application);

    private async Task<Setup>
        CreateApplicationAsync()
    {
        var identity =
            await CreateCitizenAsync();

        var request =
            Authenticated(
                HttpMethod.Post,
                "/api/v1/applications",
                identity.AccessToken);

        request.Content =
            JsonContent.Create(
                new CreateApplicationRequest(
                    "SMART_ID_REPLACEMENT_STOLEN",
                    null));

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var application =
            await response.Content
                .ReadFromJsonAsync<ApplicationResponse>();

        return new Setup(
            identity,
            application!);
    }

    private async Task<TestAuthenticationResponse>
        CreateCitizenAsync()
    {
        var identity =
            await IdentityTestHelper
                .RegisterCitizenAsync(factory);

        var request =
            Authenticated(
                HttpMethod.Post,
                "/api/v1/citizens/me",
                identity.AccessToken);

        request.Content =
            JsonContent.Create(
                new CreateCitizenProfileRequest(
                    null,
                    "Requirement",
                    null,
                    "Applicant",
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

        return identity;
    }

    private async Task UploadAsync(
        string token,
        Guid applicationId,
        string documentType)
    {
        var testBytes =
            Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

        var multipart =
            new MultipartFormDataContent();

        multipart.Add(
            new StringContent(
                documentType),
            "DocumentType");

        var file =
            new ByteArrayContent(
                testBytes);

        file.Headers.ContentType =
            new MediaTypeHeaderValue(
                "image/png");

        multipart.Add(
            file,
            "File",
            $"{documentType.ToLowerInvariant()}.png");

        var request =
            Authenticated(
                HttpMethod.Post,
                $"/api/v1/applications/" +
                $"{applicationId}/documents",
                token);

        request.Content =
            multipart;

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);
    }

    private async Task<DocumentReadinessResponse>
        GetReadinessAsync(
            string token,
            Guid applicationId)
    {
        using var request =
            Authenticated(
                HttpMethod.Get,
                $"/api/v1/applications/" +
                $"{applicationId}/document-readiness",
                token);

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        return
            (await response.Content
                .ReadFromJsonAsync<
                    DocumentReadinessResponse>())!;
    }

    private static HttpRequestMessage Authenticated(
        HttpMethod method,
        string uri,
        string token)
    {
        var request =
            new HttpRequestMessage(
                method,
                uri);

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);

        return request;
    }
}
