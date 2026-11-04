namespace HomeAffairs.Application.Citizens.ContactDetails;

public sealed class ContactDetailAlreadyExistsException
    : Exception
{
    public ContactDetailAlreadyExistsException()
        : base("This contact detail already exists.")
    {
    }
}
