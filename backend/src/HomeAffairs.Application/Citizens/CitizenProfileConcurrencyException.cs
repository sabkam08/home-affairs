namespace HomeAffairs.Application.Citizens;

public sealed class CitizenProfileConcurrencyException
    : Exception
{
    public CitizenProfileConcurrencyException()
        : base(
            "The citizen profile was modified by another request.")
    {
    }
}
