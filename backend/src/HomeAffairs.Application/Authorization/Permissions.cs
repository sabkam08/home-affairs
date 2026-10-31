namespace HomeAffairs.Application.Authorization;

public static class Permissions
{
    public static class Profile
    {
        public const string ReadSelf =
            "PROFILE.READ_SELF";
    }

    public static class Applications
    {
        public const string Create =
            "APPLICATION.CREATE";

        public const string ReadOwn =
            "APPLICATION.READ_OWN";

        public const string ReadAll =
            "APPLICATION.READ_ALL";

        public const string Process =
            "APPLICATION.PROCESS";

        public const string Reassign =
            "APPLICATION.REASSIGN";

        public const string Approve =
            "APPLICATION.APPROVE";
    }

    public static class Documents
    {
        public const string Read =
            "DOCUMENT.READ";

        public const string Upload =
            "DOCUMENT.UPLOAD";
    }

    public static class Identity
    {
        public const string ManageUsers =
            "IDENTITY.USER_MANAGE";

        public const string ManageRoles =
            "IDENTITY.ROLE_MANAGE";
    }

    public static class Audit
    {
        public const string Read =
            "AUDIT.READ";
    }
}
