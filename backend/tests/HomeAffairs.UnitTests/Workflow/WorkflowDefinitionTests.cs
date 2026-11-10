using FluentAssertions;
using HomeAffairs.Domain.Workflow;

namespace HomeAffairs.UnitTests.Workflow;

public sealed class WorkflowDefinitionTests
{
    [Fact]
    public void Workflow_should_return_initial_state()
    {
        var initial =
            new WorkflowState
            {
                StateCode = "DRAFT",
                IsInitial = true
            };

        var workflow =
            new WorkflowDefinition
            {
                States =
                [
                    initial,
                    new WorkflowState
                    {
                        StateCode = "SUBMITTED"
                    }
                ]
            };

        workflow.InitialState
            .Should()
            .BeSameAs(initial);
    }

    [Fact]
    public void Allowed_transition_should_return_true()
    {
        var workflow =
            CreateWorkflow();

        workflow.IsTransitionAllowed(
                "DRAFT",
                "SUBMITTED")
            .Should()
            .BeTrue();
    }

    [Fact]
    public void Unknown_transition_should_return_false()
    {
        var workflow =
            CreateWorkflow();

        workflow.IsTransitionAllowed(
                "DRAFT",
                "APPROVED")
            .Should()
            .BeFalse();
    }

    [Fact]
    public void Find_transition_should_return_action()
    {
        var workflow =
            CreateWorkflow();

        var transition =
            workflow.FindTransition(
                "IN_PROGRESS",
                "REQUEST_INFORMATION");

        transition.Should().NotBeNull();

        transition!.ToStateCode
            .Should()
            .Be("AWAITING_INFORMATION");

        transition.RequiresReason
            .Should()
            .BeTrue();

        transition.RequiredPermission
            .Should()
            .Be("APPLICATION.PROCESS");
    }

    [Fact]
    public void Inactive_transition_should_not_be_allowed()
    {
        var workflow =
            new WorkflowDefinition
            {
                Transitions =
                [
                    new WorkflowTransition
                    {
                        FromStateCode =
                            "DRAFT",

                        ToStateCode =
                            "SUBMITTED",

                        IsActive =
                            false
                    }
                ]
            };

        workflow.IsTransitionAllowed(
                "DRAFT",
                "SUBMITTED")
            .Should()
            .BeFalse();
    }

    private static WorkflowDefinition
        CreateWorkflow()
    {
        return new WorkflowDefinition
        {
            Transitions =
            [
                new WorkflowTransition
                {
                    FromStateCode =
                        "DRAFT",

                    ToStateCode =
                        "SUBMITTED",

                    ActionCode =
                        "SUBMIT",

                    RequiredPermission =
                        "APPLICATION.CREATE",

                    IsActive =
                        true
                },

                new WorkflowTransition
                {
                    FromStateCode =
                        "IN_PROGRESS",

                    ToStateCode =
                        "AWAITING_INFORMATION",

                    ActionCode =
                        "REQUEST_INFORMATION",

                    RequiredPermission =
                        "APPLICATION.PROCESS",

                    RequiresReason =
                        true,

                    IsActive =
                        true
                }
            ]
        };
    }
}
