namespace HomeAffairs.Application.Abstractions.Citizens;

public sealed class CitizenConcurrencyException
    : Exception
{
    public CitizenConcurrencyException()
        : base(
            "The citizen profile was modified concurrently.")
    {
    }
}
