namespace NServiceBus.Unicast.Transport
{
    using System;

    /// <summary>
    /// Defines the event data for the failed message processing event
    /// </summary>
    public class FailedMessageProcessingEventArgs : EventArgs
    {
        /// <summary>
        /// The exception that caused the processing to fail
        /// </summary>
        public Exception Reason { get; private set; }

        /// <summary>
        /// Gets the message received.
        /// </summary>
        public TransportMessage Message { get; private set; }

        /// <summary>
        /// Initialized the event arg with the actual exception
        /// </summary>
        /// <param name="m"></param>
        /// <param name="ex"></param>
        public FailedMessageProcessingEventArgs(TransportMessage m, Exception ex)
        {
            Message = m;
            Reason = ex;
        }
    }
}