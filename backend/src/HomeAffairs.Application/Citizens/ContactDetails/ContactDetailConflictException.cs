namespace HomeAffairs.Application.Citizens.ContactDetails;

public sealed class ContactDetailConflictException
    : Exception
{
    public ContactDetailConflictException()
        : base(
            "The contact detail was modified by another request.")
    {
    }
}
