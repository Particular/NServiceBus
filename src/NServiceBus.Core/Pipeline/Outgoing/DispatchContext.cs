namespace NServiceBus.TransportDispatch
{
    using Pipeline;
    using Transports;

    /// <summary>
    /// Context for the dispatch part of the pipeline.
    /// </summary>
    public class DispatchContext : BehaviorContext
    {
        /// <summary>
        /// Initializes the context with the message to be dispatched.
        /// </summary>
        public DispatchContext(OutgoingMessage messageToDispatch, BehaviorContext context) : base(context)
        {
            Message = messageToDispatch;
        }

        /// <summary>
        /// The message to dispatch the the transport.
        /// </summary>
        public OutgoingMessage Message { get; private set; }
    }
}