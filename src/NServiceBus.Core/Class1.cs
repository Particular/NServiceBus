using NServiceBus.Extensibility;

/// <summary>
/// test.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// test.
    /// </summary>
    public static ContextBag GetRootContext(this ContextBag incomingContext)
    {
        ContextBag current = incomingContext;
        while (current.parentBag != null)
        {
            current = current.parentBag;
        }

        return current;
    }
}