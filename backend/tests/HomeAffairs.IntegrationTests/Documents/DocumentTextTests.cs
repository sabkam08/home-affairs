using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentAssertions;
using HomeAffairs.Contracts.Applications;
using HomeAffairs.Contracts.Citizens;
using HomeAffairs.Contracts.Documents;
using HomeAffairs.IntegrationTests.Identity;
using HomeAffairs.IntegrationTests.Infrastructure;

namespace HomeAffairs.IntegrationTests.Documents;

[Collection(IntegrationTestCollection.Name)]
public sealed class DocumentTextTests(
    HomeAffairsApiFactory factory)
{
    private static readonly byte[] TestPng =
        Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

    [Fact]
    public async Task DOCX_text_should_be_extracted()
    {
        var setup =
            await CreateApplicationAsync();

        const string expectedText =
            "Home Affairs document extraction integration test.";

        var docx =
            CreateTestDocx(expectedText);

        var document =
            await UploadAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                "SUPPORTING_DOCUMENT",
                "extraction-test.docx",
                docx,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        var extraction =
            await ExtractTextAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                document.DocumentId);

        extraction.ExtractionStatus
            .Should()
            .Be("EXTRACTED");

        extraction.ExtractionMethod
            .Should()
            .Be("DOCX");

        extraction.ExtractedText
            .Should()
            .Contain(expectedText);

        extraction.CharacterCount
            .Should()
            .BeGreaterThan(0);

        extraction.ProcessedAtUtc
            .Should()
            .NotBeNull();

        extraction.ErrorMessage
            .Should()
            .BeNull();
    }

    [Fact]
    public async Task PNG_should_be_marked_OCR_REQUIRED()
    {
        var setup =
            await CreateApplicationAsync();

        var document =
            await UploadAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                "PROOF_OF_ADDRESS",
                "ocr-required.png",
                TestPng,
                "image/png");

        var extraction =
            await ExtractTextAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                document.DocumentId);

        extraction.ExtractionStatus
            .Should()
            .Be("OCR_REQUIRED");

        extraction.ExtractionMethod
            .Should()
            .BeNull();

        extraction.ExtractedText
            .Should()
            .BeNull();

        extraction.CharacterCount
            .Should()
            .Be(0);

        extraction.ProcessedAtUtc
            .Should()
            .NotBeNull();
    }

    [Fact]
    public async Task Search_should_find_extracted_DOCX()
    {
        var setup =
            await CreateApplicationAsync();

        var document =
            await UploadAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                "SUPPORTING_DOCUMENT",
                "searchable.docx",
                CreateTestDocx(
                    "The applicant supplied a Cape Town residential declaration."),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        await ExtractTextAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            document.DocumentId);

        var results =
            await SearchAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                "Cape Town");

        results
            .Should()
            .ContainSingle();

        results[0].DocumentId
            .Should()
            .Be(document.DocumentId);

        results[0].ExtractionStatus
            .Should()
            .Be("EXTRACTED");

        results[0].ExtractionMethod
            .Should()
            .Be("DOCX");
    }

    [Fact]
    public async Task Nonmatching_search_should_return_empty_result()
    {
        var setup =
            await CreateApplicationAsync();

        var document =
            await UploadAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                "SUPPORTING_DOCUMENT",
                "nonmatching.docx",
                CreateTestDocx(
                    "Identity verification supporting information."),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        await ExtractTextAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            document.DocumentId);

        var results =
            await SearchAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                "Pretoria");

        results
            .Should()
            .BeEmpty();
    }

    [Fact]
    public async Task Search_should_not_return_OCR_REQUIRED_image()
    {
        var setup =
            await CreateApplicationAsync();

        var image =
            await UploadAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                "PROOF_OF_ADDRESS",
                "address-image.png",
                TestPng,
                "image/png");

        var extraction =
            await ExtractTextAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                image.DocumentId);

        extraction.ExtractionStatus
            .Should()
            .Be("OCR_REQUIRED");

        var results =
            await SearchAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                "address");

        results
            .Should()
            .NotContain(result =>
                result.DocumentId ==
                image.DocumentId);
    }

    [Fact]
    public async Task Extracted_text_should_be_persisted()
    {
        var setup =
            await CreateApplicationAsync();

        const string expectedText =
            "Persisted Home Affairs extraction evidence.";

        var document =
            await UploadAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                "SUPPORTING_DOCUMENT",
                "persistent.docx",
                CreateTestDocx(expectedText),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        await ExtractTextAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            document.DocumentId);

        var persistedText =
            await factory.QueryScalarAsync<string>(
                $"""
                 SELECT ExtractedText
                 FROM [documents].DocumentTextExtractions
                 WHERE DocumentId =
                     '{document.DocumentId}';
                 """);

        persistedText
            .Should()
            .Contain(expectedText);

        var persistedStatus =
            await factory.QueryScalarAsync<string>(
                $"""
                 SELECT ExtractionStatus
                 FROM [documents].DocumentTextExtractions
                 WHERE DocumentId =
                     '{document.DocumentId}';
                 """);

        persistedStatus
            .Should()
            .Be("EXTRACTED");
    }

    [Fact]
    public async Task Get_text_should_return_previously_extracted_text()
    {
        var setup =
            await CreateApplicationAsync();

        const string expectedText =
            "Retrieval after extraction works correctly.";

        var document =
            await UploadAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                "SUPPORTING_DOCUMENT",
                "retrieval.docx",
                CreateTestDocx(expectedText),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        await ExtractTextAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            document.DocumentId);

        using var request =
            Authenticated(
                HttpMethod.Get,
                $"/api/v1/applications/" +
                $"{setup.Application.ApplicationId}/" +
                $"documents/{document.DocumentId}/text",
                setup.Identity.AccessToken);

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var extraction =
            await response.Content
                .ReadFromJsonAsync<
                    DocumentTextResponse>();

        extraction.Should().NotBeNull();

        extraction!.ExtractionStatus
            .Should()
            .Be("EXTRACTED");

        extraction.ExtractedText
            .Should()
            .Contain(expectedText);
    }

    [Fact]
    public async Task Other_citizen_should_not_extract_document()
    {
        var owner =
            await CreateApplicationAsync();

        var document =
            await UploadAsync(
                owner.Identity.AccessToken,
                owner.Application.ApplicationId,
                "SUPPORTING_DOCUMENT",
                "private.docx",
                CreateTestDocx(
                    "Private applicant evidence."),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        var other =
            await CreateCitizenAsync();

        using var request =
            Authenticated(
                HttpMethod.Post,
                $"/api/v1/applications/" +
                $"{owner.Application.ApplicationId}/" +
                $"documents/{document.DocumentId}/extract-text",
                other.AccessToken);

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Other_citizen_should_not_read_extracted_text()
    {
        var owner =
            await CreateApplicationAsync();

        var document =
            await UploadAsync(
                owner.Identity.AccessToken,
                owner.Application.ApplicationId,
                "SUPPORTING_DOCUMENT",
                "private-text.docx",
                CreateTestDocx(
                    "Private extraction content."),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        await ExtractTextAsync(
            owner.Identity.AccessToken,
            owner.Application.ApplicationId,
            document.DocumentId);

        var other =
            await CreateCitizenAsync();

        using var request =
            Authenticated(
                HttpMethod.Get,
                $"/api/v1/applications/" +
                $"{owner.Application.ApplicationId}/" +
                $"documents/{document.DocumentId}/text",
                other.AccessToken);

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Other_citizen_should_not_search_application_documents()
    {
        var owner =
            await CreateApplicationAsync();

        var document =
            await UploadAsync(
                owner.Identity.AccessToken,
                owner.Application.ApplicationId,
                "SUPPORTING_DOCUMENT",
                "private-search.docx",
                CreateTestDocx(
                    "Secret searchable Home Affairs evidence."),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        await ExtractTextAsync(
            owner.Identity.AccessToken,
            owner.Application.ApplicationId,
            document.DocumentId);

        var other =
            await CreateCitizenAsync();

        using var request =
            Authenticated(
                HttpMethod.Get,
                $"/api/v1/applications/" +
                $"{owner.Application.ApplicationId}/" +
                "document-search?q=Secret",
                other.AccessToken);

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Version_search_should_use_only_active_latest_version()
    {
        var setup =
            await CreateApplicationAsync();

        var versionOne =
            await UploadAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                "SUPPORTING_DOCUMENT",
                "version-1.docx",
                CreateTestDocx(
                    "Legacy searchable phrase AlphaDocument."),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        await ExtractTextAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            versionOne.DocumentId);

        var versionTwoBytes =
            CreateTestDocx(
                "Current searchable phrase BetaDocument.");

        var versionResponse =
            await UploadVersionAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                versionOne.DocumentId,
                versionOne.RowVersion,
                "version-2.docx",
                versionTwoBytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        versionResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var versionTwo =
            await versionResponse.Content
                .ReadFromJsonAsync<DocumentResponse>();

        versionTwo.Should().NotBeNull();

        await ExtractTextAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            versionTwo!.DocumentId);

        /*
            Version one's extraction still exists in SQL,
            but version one is inactive and must not appear
            in normal text search.
        */
        var oldResults =
            await SearchAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                "AlphaDocument");

        oldResults
            .Should()
            .BeEmpty();

        var newResults =
            await SearchAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                "BetaDocument");

        newResults
            .Should()
            .ContainSingle();

        newResults[0].DocumentId
            .Should()
            .Be(versionTwo.DocumentId);

        newResults[0].VersionNumber
            .Should()
            .Be(2);
    }

    [Fact]
    public async Task Search_term_shorter_than_two_characters_should_return_bad_request()
    {
        var setup =
            await CreateApplicationAsync();

        using var request =
            Authenticated(
                HttpMethod.Get,
                $"/api/v1/applications/" +
                $"{setup.Application.ApplicationId}/" +
                "document-search?q=A",
                setup.Identity.AccessToken);

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
                .ReadFromJsonAsync<
                    ApplicationResponse>();

        application.Should().NotBeNull();

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
                    "Extraction",
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

    private async Task<DocumentResponse>
        UploadAsync(
            string token,
            Guid applicationId,
            string documentType,
            string fileName,
            byte[] content,
            string contentType)
    {
        using var multipart =
            new MultipartFormDataContent();

        multipart.Add(
            new StringContent(
                documentType),
            "DocumentType");

        var file =
            new ByteArrayContent(content);

        file.Headers.ContentType =
            new MediaTypeHeaderValue(
                contentType);

        multipart.Add(
            file,
            "File",
            fileName);

        using var request =
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

        var document =
            await response.Content
                .ReadFromJsonAsync<
                    DocumentResponse>();

        document.Should().NotBeNull();

        return document!;
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
            new StringContent(
                rowVersion),
            "RowVersion");

        var file =
            new ByteArrayContent(
                content);

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

    private async Task<DocumentTextResponse>
        ExtractTextAsync(
            string token,
            Guid applicationId,
            Guid documentId)
    {
        using var request =
            Authenticated(
                HttpMethod.Post,
                $"/api/v1/applications/" +
                $"{applicationId}/documents/" +
                $"{documentId}/extract-text",
                token);

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var extraction =
            await response.Content
                .ReadFromJsonAsync<
                    DocumentTextResponse>();

        extraction.Should().NotBeNull();

        return extraction!;
    }

    private async Task<DocumentSearchResponse[]>
        SearchAsync(
            string token,
            Guid applicationId,
            string searchText)
    {
        using var request =
            Authenticated(
                HttpMethod.Get,
                $"/api/v1/applications/" +
                $"{applicationId}/" +
                "document-search?q=" +
                Uri.EscapeDataString(
                    searchText),
                token);

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var results =
            await response.Content
                .ReadFromJsonAsync<
                    DocumentSearchResponse[]>();

        results.Should().NotBeNull();

        return results!;
    }

    private static byte[] CreateTestDocx(
        string text)
    {
        using var stream =
            new MemoryStream();

        using (
            var document =
                WordprocessingDocument.Create(
                    stream,
                    WordprocessingDocumentType.Document,
                    true))
        {
            var mainPart =
                document.AddMainDocumentPart();

            mainPart.Document =
                new DocumentFormat.OpenXml
                    .Wordprocessing.Document(
                        new Body(
                            new Paragraph(
                                new Run(
                                    new Text(
                                        text)))));
        }

        return stream.ToArray();
    }

    private static HttpRequestMessage
        Authenticated(
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
