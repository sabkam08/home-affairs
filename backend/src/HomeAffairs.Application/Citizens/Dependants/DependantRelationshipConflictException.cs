namespace HomeAffairs.Application.Citizens.Dependants;

public sealed class DependantRelationshipConflictException
    : Exception
{
    public DependantRelationshipConflictException()
        : base(
            "The dependant relationship was modified " +
            "by another request.")
    {
    }
}
