namespace HomeAffairs.Domain.Citizens;

public static class ContactTypes
{
    public const string Email =
        "EMAIL";

    public const string Mobile =
        "MOBILE";

    public const string Landline =
        "LANDLINE";

    public const string Alternative =
        "ALTERNATIVE";

    public static readonly IReadOnlySet<string>
        All =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase)
            {
                Email,
                Mobile,
                Landline,
                Alternative
            };
}
