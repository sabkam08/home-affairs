using FluentAssertions;
using HomeAffairs.Domain.Workflow;
using HomeAffairs.IntegrationTests.Infrastructure;

namespace HomeAffairs.IntegrationTests.Workflow;

[Collection(IntegrationTestCollection.Name)]
public sealed class WorkflowWorkItemTests(
    HomeAffairsApiFactory factory)
{
    [Fact]
    public async Task In_progress_application_should_have_one_active_practitioner_work_item()
    {
        var activeCount =
            await factory.QueryScalarAsync<int>(
                """
                SELECT COUNT(1)
                FROM [workflow].WorkItems AS wi
                INNER JOIN [applications].Applications AS a
                    ON a.ApplicationId =
                        wi.ApplicationId
                WHERE a.Status = 'IN_PROGRESS'
                  AND wi.WorkItemType =
                      'PRACTITIONER_REVIEW'
                  AND wi.Status IN
                  (
                      'OPEN',
                      'CLAIMED'
                  );
                """);

        var inProgressCount =
            await factory.QueryScalarAsync<int>(
                """
                SELECT COUNT(1)
                FROM [applications].Applications
                WHERE Status = 'IN_PROGRESS';
                """);

        activeCount
            .Should()
            .Be(inProgressCount);
    }

    [Fact]
    public async Task Awaiting_approval_should_have_supervisor_work_item()
    {
        var missingCount =
            await factory.QueryScalarAsync<int>(
                """
                SELECT COUNT(1)
                FROM [applications].Applications AS a
                WHERE a.Status =
                    'AWAITING_APPROVAL'
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM [workflow].WorkItems AS wi
                      WHERE wi.ApplicationId =
                          a.ApplicationId
                        AND wi.WorkItemType =
                            'SUPERVISOR_APPROVAL'
                        AND wi.Status =
                            'OPEN'
                  );
                """);

        missingCount
            .Should()
            .Be(0);
    }

    [Fact]
    public async Task Awaiting_information_should_have_no_active_work_item()
    {
        var count =
            await factory.QueryScalarAsync<int>(
                """
                SELECT COUNT(1)
                FROM [workflow].WorkItems AS wi
                INNER JOIN [applications].Applications AS a
                    ON a.ApplicationId =
                        wi.ApplicationId
                WHERE a.Status =
                    'AWAITING_INFORMATION'
                  AND wi.Status IN
                  (
                      'OPEN',
                      'CLAIMED'
                  );
                """);

        count.Should().Be(0);
    }

    [Fact]
    public async Task Terminal_applications_should_have_no_active_work_items()
    {
        var count =
            await factory.QueryScalarAsync<int>(
                """
                SELECT COUNT(1)
                FROM [workflow].WorkItems AS wi
                INNER JOIN [applications].Applications AS a
                    ON a.ApplicationId =
                        wi.ApplicationId
                WHERE a.Status IN
                (
                    'APPROVED',
                    'REJECTED',
                    'CANCELLED',
                    'COMPLETED'
                )
                AND wi.Status IN
                (
                    'OPEN',
                    'CLAIMED'
                );
                """);

        count.Should().Be(0);
    }

    [Fact]
    public async Task Application_should_never_have_more_than_one_active_work_item()
    {
        var violations =
            await factory.QueryScalarAsync<int>(
                """
                SELECT COUNT(1)
                FROM
                (
                    SELECT
                        ApplicationId
                    FROM [workflow].WorkItems
                    WHERE Status IN
                    (
                        'OPEN',
                        'CLAIMED'
                    )
                    GROUP BY
                        ApplicationId
                    HAVING COUNT(1) > 1
                ) AS violations;
                """);

        violations.Should().Be(0);
    }
}
