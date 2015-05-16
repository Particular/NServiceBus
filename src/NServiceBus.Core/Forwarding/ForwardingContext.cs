namespace NServiceBus.Forwarding
{
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    class ForwardingContext : BehaviorContext
    {
        public ForwardingContext(OutgoingMessage messageToForward,BehaviorContext parentContext):base(parentContext)
        {
            Set(messageToForward);
        }
    }
}