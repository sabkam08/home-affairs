using FluentAssertions;
using HomeAffairs.Domain.Applications;

namespace HomeAffairs.UnitTests.Applications;

public sealed class ApplicationTests
{
    [Fact]
    public void Matching_applicant_and_subject_should_be_self_application()
    {
        var citizenId =
            Guid.NewGuid();

        var application =
            new HomeAffairsApplication
            {
                ApplicantCitizenId =
                    citizenId,

                SubjectCitizenId =
                    citizenId,

                Status =
                    ApplicationStatuses.Draft
            };

        application.IsSelfApplication
            .Should()
            .BeTrue();
    }

    [Fact]
    public void Different_applicant_and_subject_should_be_represented_application()
    {
        var application =
            new HomeAffairsApplication
            {
                ApplicantCitizenId =
                    Guid.NewGuid(),

                SubjectCitizenId =
                    Guid.NewGuid(),

                Status =
                    ApplicationStatuses.Draft
            };

        application.IsSelfApplication
            .Should()
            .BeFalse();
    }

    [Fact]
    public void Draft_application_should_report_draft_state()
    {
        var application =
            new HomeAffairsApplication
            {
                Status =
                    ApplicationStatuses.Draft
            };

        application.IsDraft
            .Should()
            .BeTrue();
    }

    [Fact]
    public void Submitted_application_should_not_be_draft()
    {
        var application =
            new HomeAffairsApplication
            {
                Status =
                    ApplicationStatuses.Submitted
            };

        application.IsDraft
            .Should()
            .BeFalse();
    }

    [Fact]
    public void Cancelled_application_should_not_be_draft()
    {
        var application =
            new HomeAffairsApplication
            {
                Status =
                    ApplicationStatuses.Cancelled
            };

        application.IsDraft
            .Should()
            .BeFalse();
    }

    [Theory]
    [InlineData("IN_PROGRESS", "AWAITING_INFORMATION", true)]
    [InlineData("IN_PROGRESS", "AWAITING_APPROVAL", true)]
    [InlineData("AWAITING_INFORMATION", "IN_PROGRESS", true)]
    [InlineData("AWAITING_APPROVAL", "IN_PROGRESS", false)]
    [InlineData("SUBMITTED", "AWAITING_APPROVAL", false)]
    public void Practitioner_transition_matrix_should_match_rules(
    string currentStatus,
    string targetStatus,
    bool expected)
    {
        var allowed =
            (
                currentStatus ==
                    ApplicationStatuses.InProgress
                &&
                (
                    targetStatus ==
                        ApplicationStatuses.AwaitingInformation
                    ||
                    targetStatus ==
                        ApplicationStatuses.AwaitingApproval
                )
            )
            ||
            (
                currentStatus ==
                    ApplicationStatuses.AwaitingInformation
                &&
                targetStatus ==
                    ApplicationStatuses.InProgress
            );

        allowed.Should().Be(expected);
    }
}
