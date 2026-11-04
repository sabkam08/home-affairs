namespace HomeAffairs.Application.Citizens.ContactDetails;

public sealed class ContactDetailNotFoundException
    : Exception
{
    public ContactDetailNotFoundException()
        : base("The contact detail could not be found.")
    {
    }
}
