namespace NServiceBus.Transports
{
    /// <summary>
    /// Defines the transport operations including the message and information how to send it.
    /// </summary>
    public class TransportOperation
    {
        OutgoingMessage message;
        DispatchOptions dispatchOptions;

        /// <summary>
        /// Creates a new transport operation.
        /// </summary>
        public TransportOperation(OutgoingMessage message, DispatchOptions dispatchOptions)
        {
            this.message = message;
            this.dispatchOptions = dispatchOptions;
        }

        /// <summary>
        /// Gets the message.
        /// </summary>
        public OutgoingMessage Message
        {
            get { return message; }
        }

        /// <summary>
        /// Gets the dispatch options.
        /// </summary>
        public DispatchOptions DispatchOptions
        {
            get { return dispatchOptions; }
        }
    }
}