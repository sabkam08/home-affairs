namespace HomeAffairs.Application.Citizens;

public sealed class CitizenProfileAlreadyExistsException
    : Exception
{
    public CitizenProfileAlreadyExistsException()
        : base(
            "A citizen profile already exists " +
            "for the current user.")
    {
    }
}
