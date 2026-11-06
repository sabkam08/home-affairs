namespace HomeAffairs.Domain.Citizens;

public static class CitizenRelationshipTypes
{
    public const string BiologicalParent =
        "BIOLOGICAL_PARENT";

    public const string AdoptiveParent =
        "ADOPTIVE_PARENT";

    public const string LegalGuardian =
        "LEGAL_GUARDIAN";

    public static readonly IReadOnlySet<string>
        All =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase)
            {
                BiologicalParent,
                AdoptiveParent,
                LegalGuardian
            };
}
