namespace HomeAffairs.Contracts.Applications;

public sealed record CreateApplicationRequest(
    string ApplicationType,
    Guid? SubjectCitizenId);
