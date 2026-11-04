namespace HomeAffairs.Application.Abstractions.Citizens;

public sealed class DuplicateContactDetailException
    : Exception
{
    public DuplicateContactDetailException()
        : base("The contact detail already exists.")
    {
    }
}
