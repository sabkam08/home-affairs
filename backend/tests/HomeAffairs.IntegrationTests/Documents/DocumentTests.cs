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
public sealed class DocumentTests(
    HomeAffairsApiFactory factory)
{
    private static readonly byte[] TestPng =
        Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

    [Fact]
    public async Task Applicant_should_upload_and_list_document()
    {
        var setup =
            await CreateApplicationAsync();

        var upload =
            await UploadAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                "PROOF_OF_ADDRESS",
                "proof.png",
                TestPng,
                "image/png");

        upload.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var document =
            await upload.Content
                .ReadFromJsonAsync<DocumentResponse>();

        document.Should().NotBeNull();

        document!.FileName
            .Should()
            .Be("proof.png");

        document.DocumentType
            .Should()
            .Be("PROOF_OF_ADDRESS");

        document.FileSizeBytes
            .Should()
            .Be(TestPng.LongLength);

        using var request =
            Authenticated(
                HttpMethod.Get,
                $"/api/v1/applications/" +
                $"{setup.Application.ApplicationId}/" +
                "documents",
                setup.Identity.AccessToken);

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var documents =
            await response.Content
                .ReadFromJsonAsync<DocumentResponse[]>();

        documents.Should().NotBeNull();

        documents!
            .Should()
            .ContainSingle(x =>
                x.DocumentId ==
                document.DocumentId);
    }

    [Fact]
    public async Task Download_should_return_original_binary_content()
    {
        var setup =
            await CreateApplicationAsync();

        var upload =
            await UploadAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                "SUPPORTING_DOCUMENT",
                "test.png",
                TestPng,
                "image/png");

        var document =
            await upload.Content
                .ReadFromJsonAsync<DocumentResponse>();

        document.Should().NotBeNull();

        using var request =
            Authenticated(
                HttpMethod.Get,
                $"/api/v1/applications/" +
                $"{setup.Application.ApplicationId}/" +
                $"documents/{document!.DocumentId}/content",
                setup.Identity.AccessToken);

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var content =
            await response.Content
                .ReadAsByteArrayAsync();

        content
            .Should()
            .Equal(TestPng);
    }

    [Fact]
    public async Task Stored_binary_length_should_match_metadata()
    {
        var setup =
            await CreateApplicationAsync();

        var upload =
            await UploadAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                "PROOF_OF_ADDRESS",
                "proof.png",
                TestPng,
                "image/png");

        var document =
            await upload.Content
                .ReadFromJsonAsync<DocumentResponse>();

        document.Should().NotBeNull();

        var storedLength =
            await factory.QueryScalarAsync<long>(
                $"""
                 SELECT DATALENGTH(Content)
                 FROM [documents].Documents
                 WHERE DocumentId =
                     '{document!.DocumentId}';
                 """);

        storedLength
            .Should()
            .Be(TestPng.LongLength);
    }

    [Fact]
    public async Task Unsupported_document_type_should_return_bad_request()
    {
        var setup =
            await CreateApplicationAsync();

        var response =
            await UploadAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                "MALWARE",
                "test.png",
                TestPng,
                "image/png");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Other_citizen_should_not_list_application_documents()
    {
        var owner =
            await CreateApplicationAsync();

        await UploadAsync(
            owner.Identity.AccessToken,
            owner.Application.ApplicationId,
            "PROOF_OF_ADDRESS",
            "proof.png",
            TestPng,
            "image/png");

        var other =
            await CreateCitizenAsync();

        using var request =
            Authenticated(
                HttpMethod.Get,
                $"/api/v1/applications/" +
                $"{owner.Application.ApplicationId}/documents",
                other.AccessToken);

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Other_citizen_should_not_download_application_document()
    {
        var owner =
            await CreateApplicationAsync();

        var upload =
            await UploadAsync(
                owner.Identity.AccessToken,
                owner.Application.ApplicationId,
                "PROOF_OF_ADDRESS",
                "private-proof.png",
                TestPng,
                "image/png");

        upload.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var document =
            await upload.Content
                .ReadFromJsonAsync<DocumentResponse>();

        document.Should().NotBeNull();

        var other =
            await CreateCitizenAsync();

        using var request =
            Authenticated(
                HttpMethod.Get,
                $"/api/v1/applications/" +
                $"{owner.Application.ApplicationId}/" +
                $"documents/{document!.DocumentId}/content",
                other.AccessToken);

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Stored_sha256_should_match_uploaded_binary()
    {
        var setup =
            await CreateApplicationAsync();

        var response =
            await UploadAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                "PROOF_OF_ADDRESS",
                "hash-test.png",
                TestPng,
                "image/png");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var document =
            await response.Content
                .ReadFromJsonAsync<DocumentResponse>();

        document.Should().NotBeNull();

        var expectedHash =
            Convert
                .ToHexString(
                    System.Security.Cryptography
                        .SHA256.HashData(TestPng))
                .ToLowerInvariant();

        document!.ContentHash
            .Should()
            .Be(expectedHash);
    }

    [Fact]
    public async Task Unsupported_content_type_should_return_bad_request()
    {
        var setup =
            await CreateApplicationAsync();

        var response =
            await UploadAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                "SUPPORTING_DOCUMENT",
                "payload.exe",
                TestPng,
                "application/octet-stream");

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
                    "SMART_ID_CORRECTION",
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
                    "Document",
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

    private async Task<HttpResponseMessage>
        UploadAsync(
            string token,
            Guid applicationId,
            string documentType,
            string fileName,
            byte[] content,
            string contentType)
    {
        var multipart =
            new MultipartFormDataContent();

        multipart.Add(
            new StringContent(documentType),
            "DocumentType");

        var file =
            new ByteArrayContent(content);

        file.Headers.ContentType =
            new System.Net.Http.Headers
                .MediaTypeHeaderValue(
                    contentType);

        multipart.Add(
            file,
            "File",
            fileName);

        var request =
            Authenticated(
                HttpMethod.Post,
                $"/api/v1/applications/" +
                $"{applicationId}/documents",
                token);

        request.Content =
            multipart;

        return await factory.Client.SendAsync(
            request);
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
