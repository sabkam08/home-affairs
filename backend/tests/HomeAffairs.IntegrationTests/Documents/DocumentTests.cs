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

    #region Tests
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

    [Fact]
    public async Task Initial_upload_should_create_version_one()
    {
        var setup =
            await CreateApplicationAsync();

        var response =
            await UploadAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                "PROOF_OF_ADDRESS",
                "proof-v1.png",
                TestPng,
                "image/png");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var document =
            await response.Content
                .ReadFromJsonAsync<DocumentResponse>();

        document.Should().NotBeNull();

        document!.VersionNumber
            .Should()
            .Be(1);

        document.LogicalDocumentId
            .Should()
            .Be(document.DocumentId);

        document.IsActive
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task Replacement_should_create_new_version_without_overwriting_original()
    {
        var setup =
            await CreateApplicationAsync();

        var originalBytes =
            TestPng;

        var replacementBytes =
            TestPng
                .Concat(
                    new byte[] { 1, 2, 3 })
                .ToArray();

        var firstUpload =
            await UploadAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                "PROOF_OF_ADDRESS",
                "proof-v1.png",
                originalBytes,
                "image/png");

        var versionOne =
            await firstUpload.Content
                .ReadFromJsonAsync<DocumentResponse>();

        versionOne.Should().NotBeNull();

        var secondUpload =
            await UploadVersionAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                versionOne!.DocumentId,
                versionOne.RowVersion,
                "proof-v2.png",
                replacementBytes,
                "image/png");

        secondUpload.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var versionTwo =
            await secondUpload.Content
                .ReadFromJsonAsync<DocumentResponse>();

        versionTwo.Should().NotBeNull();

        versionTwo!.LogicalDocumentId
            .Should()
            .Be(versionOne.LogicalDocumentId);

        versionTwo.VersionNumber
            .Should()
            .Be(2);

        versionTwo.IsActive
            .Should()
            .BeTrue();

        var originalIsActive =
            await factory.QueryScalarAsync<bool>(
                $"""
             SELECT IsActive
             FROM [documents].Documents
             WHERE DocumentId =
                 '{versionOne.DocumentId}';
             """);

        originalIsActive
            .Should()
            .BeFalse();

        var storedOriginal =
            await factory.QueryScalarAsync<byte[]>(
                $"""
             SELECT Content
             FROM [documents].Documents
             WHERE DocumentId =
                 '{versionOne.DocumentId}';
             """);

        storedOriginal
            .Should()
            .Equal(originalBytes);
    }

    [Fact]
    public async Task Normal_list_should_return_only_latest_active_version()
    {
        var setup =
            await CreateApplicationAsync();

        var first =
            await UploadAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                "PROOF_OF_ADDRESS",
                "proof-v1.png",
                TestPng,
                "image/png");

        var versionOne =
            await first.Content
                .ReadFromJsonAsync<DocumentResponse>();

        var replacement =
            TestPng
                .Concat(
                    new byte[] { 4, 5, 6 })
                .ToArray();

        var second =
            await UploadVersionAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                versionOne!.DocumentId,
                versionOne.RowVersion,
                "proof-v2.png",
                replacement,
                "image/png");

        var versionTwo =
            await second.Content
                .ReadFromJsonAsync<DocumentResponse>();

        var documents =
            await GetDocumentsAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId);

        documents.Should().ContainSingle();

        documents[0].DocumentId
            .Should()
            .Be(versionTwo!.DocumentId);

        documents[0].VersionNumber
            .Should()
            .Be(2);

        documents
            .Should()
            .NotContain(document =>
                document.DocumentId ==
                versionOne.DocumentId);
    }

    [Fact]
    public async Task Version_history_should_return_all_versions_newest_first()
    {
        var setup =
            await CreateApplicationAsync();

        var first =
            await UploadAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                "PROOF_OF_ADDRESS",
                "proof-v1.png",
                TestPng,
                "image/png");

        var versionOne =
            await first.Content
                .ReadFromJsonAsync<DocumentResponse>();

        var replacement =
            TestPng
                .Concat(
                    new byte[] { 7, 8, 9 })
                .ToArray();

        var second =
            await UploadVersionAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                versionOne!.DocumentId,
                versionOne.RowVersion,
                "proof-v2.png",
                replacement,
                "image/png");

        var versionTwo =
            await second.Content
                .ReadFromJsonAsync<DocumentResponse>();

        var versions =
            await GetVersionsAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                versionTwo!.DocumentId);

        versions.Should().HaveCount(2);

        versions[0].VersionNumber
            .Should()
            .Be(2);

        versions[1].VersionNumber
            .Should()
            .Be(1);

        versions[0].LogicalDocumentId
            .Should()
            .Be(versions[1].LogicalDocumentId);
    }

    [Fact]
    public async Task Identical_replacement_should_return_conflict()
    {
        var setup =
            await CreateApplicationAsync();

        var first =
            await UploadAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                "PROOF_OF_ADDRESS",
                "proof.png",
                TestPng,
                "image/png");

        var document =
            await first.Content
                .ReadFromJsonAsync<DocumentResponse>();

        var duplicate =
            await UploadVersionAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                document!.DocumentId,
                document.RowVersion,
                "same-proof.png",
                TestPng,
                "image/png");

        duplicate.StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Stale_document_replacement_should_return_conflict()
    {
        var setup =
            await CreateApplicationAsync();

        var first =
            await UploadAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                "PROOF_OF_ADDRESS",
                "proof-v1.png",
                TestPng,
                "image/png");

        var versionOne =
            await first.Content
                .ReadFromJsonAsync<DocumentResponse>();

        var secondBytes =
            TestPng
                .Concat(
                    new byte[] { 10 })
                .ToArray();

        var firstReplacement =
            await UploadVersionAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                versionOne!.DocumentId,
                versionOne.RowVersion,
                "proof-v2.png",
                secondBytes,
                "image/png");

        firstReplacement.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var staleBytes =
            TestPng
                .Concat(
                    new byte[] { 11 })
                .ToArray();

        var stale =
            await UploadVersionAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                versionOne.DocumentId,
                versionOne.RowVersion,
                "proof-stale.png",
                staleBytes,
                "image/png");

        stale.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Stale_current_version_row_version_should_return_conflict()
    {
        var setup =
            await CreateApplicationAsync();

        var first =
            await UploadAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                "PROOF_OF_ADDRESS",
                "proof.png",
                TestPng,
                "image/png");

        var current =
            await first.Content
                .ReadFromJsonAsync<DocumentResponse>();

        var staleRowVersion =
            Convert.ToBase64String(
                new byte[8]);

        var replacement =
            TestPng
                .Concat(
                    new byte[] { 12 })
                .ToArray();

        var response =
            await UploadVersionAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                current!.DocumentId,
                staleRowVersion,
                "proof-new.png",
                replacement,
                "image/png");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_should_deactivate_latest_version_and_preserve_history()
    {
        var setup =
            await CreateApplicationAsync();

        var first =
            await UploadAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                "PROOF_OF_ADDRESS",
                "proof-v1.png",
                TestPng,
                "image/png");

        var versionOne =
            await first.Content
                .ReadFromJsonAsync<DocumentResponse>();

        var replacement =
            TestPng
                .Concat(
                    new byte[] { 13, 14 })
                .ToArray();

        var second =
            await UploadVersionAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                versionOne!.DocumentId,
                versionOne.RowVersion,
                "proof-v2.png",
                replacement,
                "image/png");

        var versionTwo =
            await second.Content
                .ReadFromJsonAsync<DocumentResponse>();

        var delete =
            await DeleteDocumentAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                versionTwo!.DocumentId,
                versionTwo.RowVersion);

        delete.StatusCode
            .Should()
            .Be(HttpStatusCode.NoContent);

        var documents =
            await GetDocumentsAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId);

        documents.Should().BeEmpty();

        var versionCount =
            await factory.QueryScalarAsync<int>(
                $"""
             SELECT COUNT(1)
             FROM [documents].Documents
             WHERE LogicalDocumentId =
                 '{versionTwo.LogicalDocumentId}';
             """);

        versionCount.Should().Be(2);

        var activeCount =
            await factory.QueryScalarAsync<int>(
                $"""
             SELECT COUNT(1)
             FROM [documents].Documents
             WHERE LogicalDocumentId =
                 '{versionTwo.LogicalDocumentId}'
               AND IsActive = 1;
             """);

        activeCount.Should().Be(0);
    }

    [Fact]
    public async Task Other_citizen_should_not_create_document_version()
    {
        var owner =
            await CreateApplicationAsync();

        var first =
            await UploadAsync(
                owner.Identity.AccessToken,
                owner.Application.ApplicationId,
                "PROOF_OF_ADDRESS",
                "private-proof.png",
                TestPng,
                "image/png");

        var document =
            await first.Content
                .ReadFromJsonAsync<DocumentResponse>();

        var other =
            await CreateCitizenAsync();

        var replacement =
            TestPng
                .Concat(
                    new byte[] { 15 })
                .ToArray();

        var response =
            await UploadVersionAsync(
                other.AccessToken,
                owner.Application.ApplicationId,
                document!.DocumentId,
                document.RowVersion,
                "unauthorized-version.png",
                replacement,
                "image/png");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);
    }
#endregion

    #region Helpers
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

    private async Task<HttpResponseMessage>
    UploadVersionAsync(
        string token,
        Guid applicationId,
        Guid documentId,
        string rowVersion,
        string fileName,
        byte[] content,
        string contentType)
    {
        var multipart =
            new MultipartFormDataContent();

        multipart.Add(
            new StringContent(rowVersion),
            "RowVersion");

        var file =
            new ByteArrayContent(content);

        file.Headers.ContentType =
            new MediaTypeHeaderValue(
                contentType);

        multipart.Add(
            file,
            "File",
            fileName);

        var request =
            Authenticated(
                HttpMethod.Post,
                $"/api/v1/applications/" +
                $"{applicationId}/documents/" +
                $"{documentId}/versions",
                token);

        request.Content =
            multipart;

        return await factory.Client.SendAsync(
            request);
    }

    private async Task<DocumentResponse[]>
    GetVersionsAsync(
        string token,
        Guid applicationId,
        Guid documentId)
    {
        using var request =
            Authenticated(
                HttpMethod.Get,
                $"/api/v1/applications/" +
                $"{applicationId}/documents/" +
                $"{documentId}/versions",
                token);

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var versions =
            await response.Content
                .ReadFromJsonAsync<
                    DocumentResponse[]>();

        versions.Should().NotBeNull();

        return versions!;
    }

    private async Task<DocumentResponse[]>
    GetDocumentsAsync(
        string token,
        Guid applicationId)
    {
        using var request =
            Authenticated(
                HttpMethod.Get,
                $"/api/v1/applications/" +
                $"{applicationId}/documents",
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
                    DocumentResponse[]>())!;
    }

    private async Task<HttpResponseMessage>
    DeleteDocumentAsync(
        string token,
        Guid applicationId,
        Guid documentId,
        string rowVersion)
    {
        var request =
            Authenticated(
                HttpMethod.Delete,
                $"/api/v1/applications/" +
                $"{applicationId}/documents/" +
                $"{documentId}",
                token);

        request.Content =
            JsonContent.Create(
                new DeleteDocumentRequest(
                    rowVersion));

        return await factory.Client.SendAsync(
            request);
    }
    #endregion
}
