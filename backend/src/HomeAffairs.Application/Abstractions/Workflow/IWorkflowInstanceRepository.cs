using HomeAffairs.Domain.Workflow;

namespace HomeAffairs.Application.Abstractions.Workflow;

public interface IWorkflowInstanceRepository
{
	Task<WorkflowInstance?>
		GetByApplicationIdAsync(
			Guid applicationId,
			CancellationToken cancellationToken);

	Task<WorkflowInstance?>
		GetForApplicantAsync(
			Guid applicationId,
			Guid userId,
			CancellationToken cancellationToken);

	Task<IReadOnlyCollection<WorkflowTransition>>
		GetAvailableTransitionsAsync(
			Guid applicationId,
			CancellationToken cancellationToken);

	Task<WorkflowInstance> SynchronizeStateAsync(
		Guid applicationId,
		string expectedCurrentState,
		string targetState,
		byte[] expectedRowVersion,
		DateTimeOffset changedAtUtc,
		CancellationToken cancellationToken);
}
