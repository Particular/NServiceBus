namespace NServiceBus
{
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    /// <summary>
    /// Special context for unit of work style outgoing operations which cannot be interecepted with a behavior. 
    /// Therefore there is no associated public interface available.
    /// </summary>
    class TransportSendContext : BehaviorContext
    {
        public TransportSendContext(TransportTransaction transportTransaction, IBehaviorContext parentContext)
            : base(parentContext)
        {
            Set(transportTransaction);
        }
    }
}