namespace HomeAffairs.Api.Authorization;

public static class AuthorizationPolicies
{
    public const string ProfileReadSelf =
        "Permission:PROFILE.READ_SELF";

    public const string ApplicationCreate =
        "Permission:APPLICATION.CREATE";

    public const string ApplicationReadAll =
        "Permission:APPLICATION.READ_ALL";

    public const string ApplicationProcess =
        "Permission:APPLICATION.PROCESS";

    public const string ApplicationApprove =
        "Permission:APPLICATION.APPROVE";

    public const string ManageUsers =
        "Permission:IDENTITY.USER_MANAGE";

    public const string ManageRoles =
        "Permission:IDENTITY.ROLE_MANAGE";

    public const string AuditRead =
        "Permission:AUDIT.READ";
}
