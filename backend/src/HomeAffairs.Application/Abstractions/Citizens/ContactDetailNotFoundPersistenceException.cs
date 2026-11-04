namespace HomeAffairs.Application.Abstractions.Citizens;

public sealed class ContactDetailNotFoundPersistenceException
    : Exception
{
    public ContactDetailNotFoundPersistenceException()
        : base(
            "The contact detail could not be found.")
    {
    }
}
