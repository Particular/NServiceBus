namespace NServiceBus.TransportDispatch
{
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    /// <summary>
    /// Context for the dispatch part of the pipeline
    /// </summary>
    public class DispatchContext:BehaviorContext
    {
      
        /// <summary>
        /// Initializes the context with the message to be dispatched
        /// </summary>
        /// <param name="messageToDispatch"></param>
        /// <param name="context"></param>
        public DispatchContext(OutgoingMessage messageToDispatch,BehaviorContext context) : base(context)
        {
            Set(messageToDispatch);
        }
    }
}