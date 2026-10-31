namespace HomeAffairs.Application.Identity;

public static class Roles
{
    public const string Citizen =
        "CITIZEN";

    public const string Practitioner =
        "PRACTITIONER";

    public const string Supervisor =
        "SUPERVISOR";

    public const string Administrator =
        "ADMINISTRATOR";

    public const string SecurityAdministrator =
        "SECURITY_ADMINISTRATOR";

    public static readonly IReadOnlySet<string>
        StaffRoles =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase)
            {
                Practitioner,
                Supervisor,
                Administrator,
                SecurityAdministrator
            };
}
