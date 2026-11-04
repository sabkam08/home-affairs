namespace HomeAffairs.Application.Abstractions.Citizens;

public sealed class ContactDetailConcurrencyException
    : Exception
{
    public ContactDetailConcurrencyException()
        : base(
            "The contact detail was modified concurrently.")
    {
    }
}
