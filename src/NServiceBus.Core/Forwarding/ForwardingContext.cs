namespace NServiceBus
{
    using Pipeline;
    using Transports;

    class ForwardingContext : BehaviorContext
    {
        public OutgoingMessage Message { get; private set; }

        public string Address { get; private set; }

        public ForwardingContext(OutgoingMessage messageToForward, string address, BehaviorContext parentContext) : base(parentContext)
        {
            Message = messageToForward;
            Address = address;
        }
    }
}