namespace HomeAffairs.Domain.Citizens;

public static class AddressTypes
{
    public const string Residential =
        "RESIDENTIAL";

    public const string Postal =
        "POSTAL";

    public static readonly IReadOnlySet<string>
        All =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase)
            {
                Residential,
                Postal
            };
}
