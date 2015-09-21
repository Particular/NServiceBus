namespace NServiceBus.Transports
{
    /// <summary>
    /// Defines the transport operations including the message and information how to send it.
    /// </summary>
    public class TransportOperation
    {
        /// <summary>
        /// Creates a new transport operation.
        /// </summary>
        public TransportOperation(OutgoingMessage message, DispatchOptions dispatchOptions)
        {
            Message = message;
            DispatchOptions = dispatchOptions;
        }

        /// <summary>
        /// Gets the message.
        /// </summary>
        public OutgoingMessage Message { get; private set; }

        /// <summary>
        /// Gets the dispatch options.
        /// </summary>
        public DispatchOptions DispatchOptions { get; private set; }
    }
}