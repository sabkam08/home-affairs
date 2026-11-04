namespace HomeAffairs.Application.Citizens;

public sealed class CitizenProfileNotFoundException
    : Exception
{
    public CitizenProfileNotFoundException()
        : base("The citizen profile could not be found.")
    {
    }
}
