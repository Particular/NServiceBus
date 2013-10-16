namespace NServiceBus.Pipeline.Behaviors
{
    /// <summary>
    /// could we declaratively state context state requirements like this? (and maybe in some cases remove the need for ExecuteAfter<..>?)
    /// </summary>
    public interface RequireContextItemOfType<T>
    {
    }
}