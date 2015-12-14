namespace NServiceBus
{
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    /// <summary>
    /// Provides context for the forwarding pipeline.
    /// </summary>
    public class ForwardingContext : BehaviorContext, IForwardingContext
    {
        /// <summary>
        /// Creates a new instance of the forwarding context.
        /// </summary>
        /// <param name="messageToForward">The message to be forwarded.</param>
        /// <param name="address">The forwarding queue address.</param>
        /// <param name="parentContext">The parent context.</param>
        public ForwardingContext(OutgoingMessage messageToForward, string address, IBehaviorContext parentContext) : base(parentContext)
        {
            Message = messageToForward;
            Address = address;
        }

        /// <summary>
        /// The message to be fowarded.
        /// </summary>
        public OutgoingMessage Message { get; }


        /// <summary>
        /// The address of the forwarding queue.
        /// </summary>
        public string Address { get; }
    }
}