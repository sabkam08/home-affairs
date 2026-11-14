namespace HomeAffairs.Application.Applications;

public sealed class ActiveSmartIdApplicationException
    : Exception
{
    public ActiveSmartIdApplicationException()
        : base(
            "The subject citizen already has an " +
            "active Smart ID application.")
    {
    }
}
