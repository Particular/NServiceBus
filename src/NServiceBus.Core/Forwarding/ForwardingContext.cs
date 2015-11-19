namespace NServiceBus.Forwarding
{
    using Pipeline;
    using Transports;

    /// <summary>
    /// 
    /// </summary>
    public interface ForwardingContext : BehaviorContext
    {
        /// <summary>
        /// 
        /// </summary>
        OutgoingMessage Message { get; }

        /// <summary>
        /// 
        /// </summary>
        string Address { get; }
    }

    class ForwardingContextImpl : BehaviorContextImpl, ForwardingContext
    {
        public OutgoingMessage Message { get; private set; }

        public string Address { get; private set; }

        public ForwardingContextImpl(OutgoingMessage messageToForward, string address, BehaviorContext parentContext) : base(parentContext)
        {
            Message = messageToForward;
            Address = address;
        }
    }
}