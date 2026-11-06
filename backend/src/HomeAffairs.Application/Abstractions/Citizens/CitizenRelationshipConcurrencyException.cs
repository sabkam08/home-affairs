namespace HomeAffairs.Application.Abstractions.Citizens;

public sealed class CitizenRelationshipConcurrencyException
    : Exception
{
    public CitizenRelationshipConcurrencyException()
        : base(
            "The citizen relationship was modified concurrently.")
    {
    }
}
