using HomeAffairs.Domain.Applications;

namespace HomeAffairs.Domain.SmartId;

public static class SmartIdApplicationTypes
{
    public static readonly IReadOnlySet<string>
        All =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase)
            {
                ApplicationTypes.SmartIdNew,
                ApplicationTypes.SmartIdReplacementLost,
                ApplicationTypes.SmartIdReplacementStolen,
                ApplicationTypes.SmartIdReplacementDamaged,
                ApplicationTypes.SmartIdCorrection
            };

    public static bool IsSmartId(
        string applicationType)
    {
        return All.Contains(
            applicationType);
    }
}
