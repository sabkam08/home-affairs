namespace HomeAffairs.Domain.Documents;

public static class DocumentTypes
{
    public const string IdentityDocument =
        "IDENTITY_DOCUMENT";

    public const string BirthCertificate =
        "BIRTH_CERTIFICATE";

    public const string ProofOfAddress =
        "PROOF_OF_ADDRESS";

    public const string Affidavit =
        "AFFIDAVIT";

    public const string SupportingDocument =
        "SUPPORTING_DOCUMENT";

    public const string GuardianProof =
        "GUARDIAN_PROOF";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            IdentityDocument,
            BirthCertificate,
            ProofOfAddress,
            Affidavit,
            SupportingDocument,
            GuardianProof
        };
}
