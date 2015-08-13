namespace NServiceBus.TransportDispatch
{
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

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
            Set(messageToDispatch);
        }
    }
}