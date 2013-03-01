namespace NServiceBus.Unicast.Transport
{
    using System;

    /// <summary>
    ///     Provides data for the MessageDequeued event.
    /// </summary>
    public class TransportMessageAvailableEventArgs : EventArgs
    {
        private readonly TransportMessage message;

        /// <summary>
        ///     Default constructor for <see cref="TransportMessageAvailableEventArgs" />.
        /// </summary>
        /// <param name="m">
        ///     The received <see cref="TransportMessage" />.
        /// </param>
        public TransportMessageAvailableEventArgs(TransportMessage m)
        {
            message = m;
        }

        /// <summary>
        ///     The received <see cref="TransportMessage" />.
        /// </summary>
        public TransportMessage Message
        {
            get { return message; }
        }
    }
}