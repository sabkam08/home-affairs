using FluentAssertions;
using HomeAffairs.Domain.Workflow;

namespace HomeAffairs.UnitTests.Workflow;

public sealed class WorkflowWorkItemTests
{
    [Theory]
    [InlineData("OPEN", true)]
    [InlineData("CLAIMED", true)]
    [InlineData("COMPLETED", false)]
    [InlineData("CANCELLED", false)]
    public void Active_state_should_match_status(
        string status,
        bool expected)
    {
        var item =
            new WorkflowWorkItem
            {
                Status = status
            };

        item.IsActive
            .Should()
            .Be(expected);
    }

    [Fact]
    public void Practitioner_work_item_should_keep_assignee()
    {
        var userId =
            Guid.NewGuid();

        var item =
            new WorkflowWorkItem
            {
                WorkItemType =
                    WorkItemTypes.PractitionerReview,

                AssignedToUserId =
                    userId,

                AssignedRoleCode =
                    "PRACTITIONER"
            };

        item.AssignedToUserId
            .Should()
            .Be(userId);

        item.AssignedRoleCode
            .Should()
            .Be("PRACTITIONER");
    }
}
