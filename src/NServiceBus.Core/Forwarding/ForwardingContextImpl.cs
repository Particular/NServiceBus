namespace NServiceBus.Forwarding
{
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    class ForwardingContextImpl : BehaviorContextImpl, ForwardingContext
    {
        public ForwardingContextImpl(OutgoingMessage messageToForward, string address, BehaviorContext parentContext) : base(parentContext)
        {
            Message = messageToForward;
            Address = address;
        }

        public OutgoingMessage Message { get; private set; }

        public string Address { get; private set; }
    }
}