using HomeAffairs.Domain.Applications;

namespace HomeAffairs.Domain.Birth;

public static class BirthSubmissionRules
{
    public static bool HasRequiredDetails(
        BirthServiceDetails details)
    {
        return details.ApplicationType switch
        {
            ApplicationTypes.BirthRegistration =>
                HasCoreBirthRegistrationDetails(
                    details),

            ApplicationTypes.LateBirthRegistration =>
                HasCoreBirthRegistrationDetails(
                    details)
                &&
                HasValue(
                    details.RequestReason),

            ApplicationTypes.BirthCertificateReissue =>
                HasValue(
                    details.CurrentCertificateNumber)
                &&
                HasValue(
                    details.RequestReason),

            ApplicationTypes.BirthCertificateAmendment =>
                HasValue(
                    details.CurrentCertificateNumber)
                &&
                HasValue(
                    details.RequestReason),

            _ =>
                false
        };
    }

    private static bool
        HasCoreBirthRegistrationDetails(
            BirthServiceDetails details)
    {
        return
            HasValue(details.ChildFirstName)
            &&
            HasValue(details.ChildLastName)
            &&
            details.ChildDateOfBirth.HasValue
            &&
            HasValue(details.ChildSex)
            &&
            HasValue(details.PlaceOfBirth)
            &&
            HasValue(details.CountryOfBirth)
            &&
            HasValue(details.MotherFirstName)
            &&
            HasValue(details.MotherLastName);
    }

    private static bool HasValue(
        string? value)
    {
        return !string.IsNullOrWhiteSpace(
            value);
    }
}
