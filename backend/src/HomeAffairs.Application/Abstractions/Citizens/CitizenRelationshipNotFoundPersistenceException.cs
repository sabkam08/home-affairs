namespace HomeAffairs.Application.Abstractions.Citizens;

public sealed class CitizenRelationshipNotFoundPersistenceException
    : Exception
{
    public CitizenRelationshipNotFoundPersistenceException()
        : base(
            "The citizen relationship could not be found.")
    {
    }
}
