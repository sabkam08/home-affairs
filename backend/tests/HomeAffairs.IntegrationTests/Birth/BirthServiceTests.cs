using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HomeAffairs.Contracts.Applications;
using HomeAffairs.Contracts.Birth;
using HomeAffairs.Contracts.Citizens;
using HomeAffairs.IntegrationTests.Identity;
using HomeAffairs.IntegrationTests.Infrastructure;

namespace HomeAffairs.IntegrationTests.Birth;

[Collection(IntegrationTestCollection.Name)]
public sealed class BirthServiceTests(
    HomeAffairsApiFactory factory)
{
    private static readonly byte[] TestPng =
    Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

    [Fact]
    public async Task Applicant_should_save_birth_registration_details()
    {
        var setup =
            await CreateBirthApplicationAsync(
                "BIRTH_REGISTRATION");

        var response =
            await SaveAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                CreateRequest());

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var details =
            await response.Content
                .ReadFromJsonAsync<
                    BirthServiceDetailsResponse>();

        details.Should().NotBeNull();

        details!.ApplicationType
            .Should()
            .Be("BIRTH_REGISTRATION");

        details.ChildFirstName
            .Should()
            .Be("Baby");

        details.ChildLastName
            .Should()
            .Be("Citizen");

        details.PlaceOfBirth
            .Should()
            .Be("Cape Town");

        details.RowVersion
            .Should()
            .NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("BIRTH_REGISTRATION")]
    [InlineData("BIRTH_CERTIFICATE_REISSUE")]
    [InlineData("LATE_BIRTH_REGISTRATION")]
    [InlineData("BIRTH_CERTIFICATE_AMENDMENT")]
    public async Task All_birth_application_types_should_accept_birth_details(
        string applicationType)
    {
        var setup =
            await CreateBirthApplicationAsync(
                applicationType);

        var response =
            await SaveAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                CreateRequest());

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var details =
            await response.Content
                .ReadFromJsonAsync<
                    BirthServiceDetailsResponse>();

        details.Should().NotBeNull();

        details!.ApplicationType
            .Should()
            .Be(applicationType);
    }

    [Fact]
    public async Task Applicant_should_read_saved_birth_details()
    {
        var setup =
            await CreateBirthApplicationAsync(
                "BIRTH_REGISTRATION");

        await SaveAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            CreateRequest());

        using var request =
            Authenticated(
                HttpMethod.Get,
                $"/api/v1/applications/" +
                $"{setup.Application.ApplicationId}/birth",
                setup.Identity.AccessToken);

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var details =
            await response.Content
                .ReadFromJsonAsync<
                    BirthServiceDetailsResponse>();

        details.Should().NotBeNull();

        details!.MotherFirstName
            .Should()
            .Be("Test");

        details.MotherLastName
            .Should()
            .Be("Mother");
    }

    [Fact]
    public async Task Applicant_should_update_birth_details_with_current_row_version()
    {
        var setup =
            await CreateBirthApplicationAsync(
                "BIRTH_REGISTRATION");

        var firstResponse =
            await SaveAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                CreateRequest());

        var first =
            await firstResponse.Content
                .ReadFromJsonAsync<
                    BirthServiceDetailsResponse>();

        first.Should().NotBeNull();

        var update =
            CreateRequest(
                rowVersion:
                    first!.RowVersion,

                placeOfBirth:
                    "Stellenbosch");

        var updateResponse =
            await SaveAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                update);

        updateResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var updated =
            await updateResponse.Content
                .ReadFromJsonAsync<
                    BirthServiceDetailsResponse>();

        updated.Should().NotBeNull();

        updated!.PlaceOfBirth
            .Should()
            .Be("Stellenbosch");

        updated.RowVersion
            .Should()
            .NotBe(first.RowVersion);
    }

    [Fact]
    public async Task Stale_birth_details_update_should_return_conflict()
    {
        var setup =
            await CreateBirthApplicationAsync(
                "BIRTH_REGISTRATION");

        var firstResponse =
            await SaveAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                CreateRequest());

        var first =
            await firstResponse.Content
                .ReadFromJsonAsync<
                    BirthServiceDetailsResponse>();

        first.Should().NotBeNull();

        var originalRowVersion =
            first!.RowVersion;

        var updateResponse =
            await SaveAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                CreateRequest(
                    originalRowVersion,
                    "Stellenbosch"));

        updateResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var stale =
            await SaveAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                CreateRequest(
                    originalRowVersion,
                    "Paarl"));

        stale.StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Smart_id_application_should_reject_birth_details()
    {
        var setup =
            await CreateApplicationAsync(
                "SMART_ID_NEW");

        var response =
            await SaveAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                CreateRequest());

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Other_citizen_should_not_read_birth_details()
    {
        var owner =
            await CreateBirthApplicationAsync(
                "BIRTH_REGISTRATION");

        await SaveAsync(
            owner.Identity.AccessToken,
            owner.Application.ApplicationId,
            CreateRequest());

        var other =
            await CreateCitizenAsync();

        using var request =
            Authenticated(
                HttpMethod.Get,
                $"/api/v1/applications/" +
                $"{owner.Application.ApplicationId}/birth",
                other.AccessToken);

        var response =
            await factory.Client.SendAsync(
                request);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Other_citizen_should_not_update_birth_details()
    {
        var owner =
            await CreateBirthApplicationAsync(
                "BIRTH_REGISTRATION");

        var savedResponse =
            await SaveAsync(
                owner.Identity.AccessToken,
                owner.Application.ApplicationId,
                CreateRequest());

        var saved =
            await savedResponse.Content
                .ReadFromJsonAsync<
                    BirthServiceDetailsResponse>();

        var other =
            await CreateCitizenAsync();

        var response =
            await SaveAsync(
                other.AccessToken,
                owner.Application.ApplicationId,
                CreateRequest(
                    saved!.RowVersion,
                    "Unauthorized"));

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Submitted_birth_application_should_not_allow_detail_changes()
    {
        var setup =
            await CreateBirthApplicationAsync(
                "BIRTH_REGISTRATION");

        var savedResponse =
            await SaveAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                CreateRequest());

        var saved =
            await savedResponse.Content
                .ReadFromJsonAsync<
                    BirthServiceDetailsResponse>();

        saved.Should().NotBeNull();

        /*
            BIRTH_REGISTRATION requires:
                IDENTITY_DOCUMENT
                PROOF_OF_ADDRESS
        */

        await UploadDocumentAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            "IDENTITY_DOCUMENT");

        await UploadDocumentAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            "PROOF_OF_ADDRESS");

        var submitRequest =
            Authenticated(
                HttpMethod.Post,
                $"/api/v1/applications/" +
                $"{setup.Application.ApplicationId}/submit",
                setup.Identity.AccessToken);

        submitRequest.Content =
            JsonContent.Create(
                new ApplicationActionRequest(
                    setup.Application.RowVersion));

        var submitResponse =
            await factory.Client.SendAsync(
                submitRequest);

        submitResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var update =
            await SaveAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                CreateRequest(
                    saved!.RowVersion,
                    "Too Late"));

        update.StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Birth_application_without_details_should_not_submit()
    {
        var setup =
            await CreateBirthApplicationAsync(
                "BIRTH_REGISTRATION");

        await UploadDocumentAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            "IDENTITY_DOCUMENT");

        await UploadDocumentAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            "PROOF_OF_ADDRESS");

        var response =
            await SubmitAsync(
                setup);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Birth_application_with_complete_details_but_missing_documents_should_not_submit()
    {
        var setup =
            await CreateBirthApplicationAsync(
                "BIRTH_REGISTRATION");

        var details =
            await SaveAsync(
                setup.Identity.AccessToken,
                setup.Application.ApplicationId,
                CreateRequest());

        details.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var response =
            await SubmitAsync(
                setup);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Birth_application_with_only_one_required_document_should_not_submit()
    {
        var setup =
            await CreateBirthApplicationAsync(
                "BIRTH_REGISTRATION");

        await SaveAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            CreateRequest());

        await UploadDocumentAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            "IDENTITY_DOCUMENT");

        var response =
            await SubmitAsync(
                setup);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Complete_birth_registration_should_submit()
    {
        var setup =
            await CreateBirthApplicationAsync(
                "BIRTH_REGISTRATION");

        await SaveAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            CreateRequest());

        await UploadDocumentAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            "IDENTITY_DOCUMENT");

        await UploadDocumentAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            "PROOF_OF_ADDRESS");

        var response =
            await SubmitAsync(
                setup);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var submitted =
            await response.Content
                .ReadFromJsonAsync<
                    ApplicationResponse>();

        submitted.Should().NotBeNull();

        submitted!.Status
            .Should()
            .Be("SUBMITTED");
    }

    [Fact]
    public async Task Failed_birth_submission_should_leave_application_and_workflow_in_draft()
    {
        var setup =
            await CreateBirthApplicationAsync(
                "BIRTH_REGISTRATION");

        await SaveAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            CreateRequest());

        var response =
            await SubmitAsync(
                setup);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);

        var applicationStatus =
            await factory.QueryScalarAsync<string>(
                $"""
             SELECT Status
             FROM [applications].Applications
             WHERE ApplicationId =
                 '{setup.Application.ApplicationId}';
             """);

        applicationStatus
            .Should()
            .Be("DRAFT");

        var workflowStatus =
            await factory.QueryScalarAsync<string>(
                $"""
             SELECT CurrentStateCode
             FROM [workflow].WorkflowInstances
             WHERE ApplicationId =
                 '{setup.Application.ApplicationId}';
             """);

        workflowStatus
            .Should()
            .Be("DRAFT");
    }

    [Fact]
    public async Task Birth_certificate_reissue_requires_certificate_number_and_reason()
    {
        var setup =
            await CreateBirthApplicationAsync(
                "BIRTH_CERTIFICATE_REISSUE");

        var request =
            new SaveBirthServiceDetailsRequest(
                ChildFirstName: null,
                ChildMiddleNames: null,
                ChildLastName: null,
                ChildDateOfBirth: null,
                ChildSex: null,
                PlaceOfBirth: null,
                ProvinceOfBirth: null,
                CountryOfBirth: null,
                MotherFirstName: null,
                MotherLastName: null,
                MotherIdNumber: null,
                FatherFirstName: null,
                FatherLastName: null,
                FatherIdNumber: null,
                CurrentCertificateNumber:
                    null,
                RequestReason:
                    null,
                RowVersion:
                    null);

        await SaveAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            request);

        await UploadDocumentAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            "IDENTITY_DOCUMENT");

        var response =
            await SubmitAsync(
                setup);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Complete_birth_certificate_reissue_should_submit()
    {
        var setup =
            await CreateBirthApplicationAsync(
                "BIRTH_CERTIFICATE_REISSUE");

        var request =
            new SaveBirthServiceDetailsRequest(
                ChildFirstName: null,
                ChildMiddleNames: null,
                ChildLastName: null,
                ChildDateOfBirth: null,
                ChildSex: null,
                PlaceOfBirth: null,
                ProvinceOfBirth: null,
                CountryOfBirth: null,
                MotherFirstName: null,
                MotherLastName: null,
                MotherIdNumber: null,
                FatherFirstName: null,
                FatherLastName: null,
                FatherIdNumber: null,
                CurrentCertificateNumber:
                    "BC-TEST-2026-001",
                RequestReason:
                    "Certificate damaged and requires re-issue.",
                RowVersion:
                    null);

        await SaveAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            request);

        await UploadDocumentAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            "IDENTITY_DOCUMENT");

        var response =
            await SubmitAsync(
                setup);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Late_birth_registration_requires_reason()
    {
        var setup =
            await CreateBirthApplicationAsync(
                "LATE_BIRTH_REGISTRATION");

        var request =
            CreateRequest();

        var withoutReason =
            request with
            {
                RequestReason = null
            };

        await SaveAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            withoutReason);

        await UploadDocumentAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            "IDENTITY_DOCUMENT");

        await UploadDocumentAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            "PROOF_OF_ADDRESS");

        await UploadDocumentAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            "AFFIDAVIT");

        var response =
            await SubmitAsync(
                setup);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Complete_late_birth_registration_should_submit()
    {
        var setup =
            await CreateBirthApplicationAsync(
                "LATE_BIRTH_REGISTRATION");

        var request =
            CreateRequest() with
            {
                RequestReason =
                    "Birth was not registered within the normal process."
            };

        await SaveAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            request);

        await UploadDocumentAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            "IDENTITY_DOCUMENT");

        await UploadDocumentAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            "PROOF_OF_ADDRESS");

        await UploadDocumentAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            "AFFIDAVIT");

        var response =
            await SubmitAsync(
                setup);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Birth_certificate_amendment_requires_certificate_number_and_reason()
    {
        var setup =
            await CreateBirthApplicationAsync(
                "BIRTH_CERTIFICATE_AMENDMENT");

        var request =
            CreateRequest() with
            {
                CurrentCertificateNumber =
                    null,

                RequestReason =
                    null
            };

        await SaveAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            request);

        await UploadDocumentAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            "IDENTITY_DOCUMENT");

        await UploadDocumentAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            "BIRTH_CERTIFICATE");

        var response =
            await SubmitAsync(
                setup);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Complete_birth_certificate_amendment_should_submit()
    {
        var setup =
            await CreateBirthApplicationAsync(
                "BIRTH_CERTIFICATE_AMENDMENT");

        var request =
            CreateRequest() with
            {
                CurrentCertificateNumber =
                    "BC-AMEND-2026-001",

                RequestReason =
                    "Correction required to certificate details."
            };

        await SaveAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            request);

        await UploadDocumentAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            "IDENTITY_DOCUMENT");

        await UploadDocumentAsync(
            setup.Identity.AccessToken,
            setup.Application.ApplicationId,
            "BIRTH_CERTIFICATE");

        var response =
            await SubmitAsync(
                setup);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);
    }

    private sealed record Setup(
        TestAuthenticationResponse Identity,
        ApplicationResponse Application);

    private async Task<Setup>
        CreateBirthApplicationAsync(
            string applicationType)
    {
        return await CreateApplicationAsync(
            applicationType);
    }

    private async Task UploadDocumentAsync(
    string token,
    Guid applicationId,
    string documentType)
    {
        using var multipart =
            new MultipartFormDataContent();

        multipart.Add(
            new StringContent(
                documentType),
            "DocumentType");

        var file =
            new ByteArrayContent(
                TestPng);

        file.Headers.ContentType =
            new MediaTypeHeaderValue(
                "image/png");

        multipart.Add(
            file,
            "File",
            $"{documentType.ToLowerInvariant()}.png");

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
    }

    private async Task<HttpResponseMessage>
    SubmitAsync(
        Setup setup)
    {
        var request =
            Authenticated(
                HttpMethod.Post,
                $"/api/v1/applications/" +
                $"{setup.Application.ApplicationId}/submit",
                setup.Identity.AccessToken);

        request.Content =
            JsonContent.Create(
                new ApplicationActionRequest(
                    setup.Application.RowVersion));

        return await factory.Client.SendAsync(
            request);
    }

    private async Task<Setup>
        CreateApplicationAsync(
            string applicationType)
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
                    applicationType,
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
                    "Birth",
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
        SaveAsync(
            string token,
            Guid applicationId,
            SaveBirthServiceDetailsRequest body)
    {
        var request =
            Authenticated(
                HttpMethod.Put,
                $"/api/v1/applications/" +
                $"{applicationId}/birth",
                token);

        request.Content =
            JsonContent.Create(body);

        return await factory.Client.SendAsync(
            request);
    }

    private static SaveBirthServiceDetailsRequest
        CreateRequest(
            string? rowVersion = null,
            string placeOfBirth = "Cape Town")
    {
        return new SaveBirthServiceDetailsRequest(
            ChildFirstName:
                "Baby",

            ChildMiddleNames:
                "Integration",

            ChildLastName:
                "Citizen",

            ChildDateOfBirth:
                new DateOnly(
                    2026,
                    1,
                    15),

            ChildSex:
                "F",

            PlaceOfBirth:
                placeOfBirth,

            ProvinceOfBirth:
                "Western Cape",

            CountryOfBirth:
                "South Africa",

            MotherFirstName:
                "Test",

            MotherLastName:
                "Mother",

            MotherIdNumber:
                "9001015009087",

            FatherFirstName:
                "Test",

            FatherLastName:
                "Father",

            FatherIdNumber:
                "8801015009088",

            CurrentCertificateNumber:
                null,

            RequestReason:
                null,

            RowVersion:
                rowVersion);
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
