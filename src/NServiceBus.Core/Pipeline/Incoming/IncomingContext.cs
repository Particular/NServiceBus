namespace NServiceBus.Pipeline.Contexts
{
    /// <summary>
    /// The base interface for everything after the transport receive phase.
    /// </summary>
    public interface IncomingContext : BehaviorContext, IMessageProcessingContext
    {
    }
}