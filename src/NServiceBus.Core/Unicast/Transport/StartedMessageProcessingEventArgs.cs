namespace NServiceBus.Unicast.Transport
{
    using System;

    /// <summary>
    /// Defines the arguments passed to the event handler of the
    /// <see cref="ITransport.StartedMessageProcessing"/> event.
    /// </summary>
    public class StartedMessageProcessingEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new TransportMessageReceivedEventArgs.
        /// </summary>
        /// <param name="m">The message that was received.</param>
        public StartedMessageProcessingEventArgs(TransportMessage m)
        {
            message = m;
        }

        readonly TransportMessage message;

        /// <summary>
        /// Gets the message received.
        /// </summary>
        public TransportMessage Message
        {
            get { return message; }
        }
    }
}