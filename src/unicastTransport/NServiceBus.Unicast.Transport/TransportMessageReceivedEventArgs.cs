using System;

namespace NServiceBus.Unicast.Transport
{
    /// <summary>
    /// Defines the arguments passed to the event handler of the
    /// <see cref="ITransport.TransportMessageReceived"/> event.
    /// </summary>
    public class TransportMessageReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new TransportMessageReceivedEventArgs.
        /// </summary>
        /// <param name="m">The message that was received.</param>
        public TransportMessageReceivedEventArgs(TransportMessage m)
        {
            message = m;
        }

        private readonly TransportMessage message;

        /// <summary>
        /// Gets the message received.
        /// </summary>
        public TransportMessage Message
        {
            get { return message; }
        }
    }
}
