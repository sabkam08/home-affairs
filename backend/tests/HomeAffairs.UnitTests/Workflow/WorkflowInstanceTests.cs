using FluentAssertions;
using HomeAffairs.Domain.Workflow;

namespace HomeAffairs.UnitTests.Workflow;

public sealed class WorkflowInstanceTests
{
    [Fact]
    public void Workflow_instance_without_completed_date_should_be_active()
    {
        var instance =
            new WorkflowInstance
            {
                CurrentStateCode =
                    "IN_PROGRESS",

                CompletedAtUtc =
                    null
            };

        instance.IsCompleted
            .Should()
            .BeFalse();
    }

    [Fact]
    public void Workflow_instance_with_completed_date_should_be_complete()
    {
        var instance =
            new WorkflowInstance
            {
                CurrentStateCode =
                    "APPROVED",

                CompletedAtUtc =
                    DateTimeOffset.UtcNow
            };

        instance.IsCompleted
            .Should()
            .BeTrue();
    }

    [Fact]
    public void Workflow_instance_should_preserve_pinned_definition_version()
    {
        var instance =
            new WorkflowInstance
            {
                WorkflowDefinitionId =
                    Guid.NewGuid(),

                WorkflowVersion =
                    1,

                ApplicationType =
                    "SMART_ID_NEW"
            };

        instance.WorkflowVersion
            .Should()
            .Be(1);
    }
}
