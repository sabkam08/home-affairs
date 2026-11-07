namespace HomeAffairs.Domain.Applications;

public static class ApplicationTypes
{
    public const string BirthRegistration =
        "BIRTH_REGISTRATION";

    public const string BirthCertificateReissue =
        "BIRTH_CERTIFICATE_REISSUE";

    public const string LateBirthRegistration =
        "LATE_BIRTH_REGISTRATION";

    public const string BirthCertificateAmendment =
        "BIRTH_CERTIFICATE_AMENDMENT";

    public const string SmartIdNew =
        "SMART_ID_NEW";

    public const string SmartIdReplacementLost =
        "SMART_ID_REPLACEMENT_LOST";

    public const string SmartIdReplacementStolen =
        "SMART_ID_REPLACEMENT_STOLEN";

    public const string SmartIdReplacementDamaged =
        "SMART_ID_REPLACEMENT_DAMAGED";

    public const string SmartIdCorrection =
        "SMART_ID_CORRECTION";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            BirthRegistration,
            BirthCertificateReissue,
            LateBirthRegistration,
            BirthCertificateAmendment,
            SmartIdNew,
            SmartIdReplacementLost,
            SmartIdReplacementStolen,
            SmartIdReplacementDamaged,
            SmartIdCorrection
        };
}
