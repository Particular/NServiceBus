namespace NServiceBus
{
    using Pipeline;
    using Transport;

    class ForwardingContext : BehaviorContext, IForwardingContext
    {
        public ForwardingContext(OutgoingMessage messageToForward, string address, IBehaviorContext parentContext) : base(parentContext)
        {
            Message = messageToForward;
            Address = address;
        }

        public OutgoingMessage Message { get; }

        public string Address { get; }
    }
}