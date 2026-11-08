namespace HomeAffairs.Contracts.Applications;

public sealed record UpdateApplicationRequest(
    string ApplicationType,
    Guid? SubjectCitizenId,
    string RowVersion);
