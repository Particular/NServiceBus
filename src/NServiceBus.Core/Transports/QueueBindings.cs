namespace NServiceBus.Transports
{
    using System.Collections.Generic;

    /// <summary>
    /// Contains information about queues this endpoint is using.
    /// </summary>
    public class QueueBindings
    {
        readonly List<string> receiveAddresses = new List<string>();
        readonly List<string> sendingAddresses = new List<string>();

        /// <summary>
        /// Declares that this endpoint will be using queue with address <paramref name="address"/> for receiving.
        /// </summary>
        /// <param name="address">The address of the queue.</param>
        public void BindReceiving(string address)
        {
            receiveAddresses.Add(address);
        }

        /// <summary>
        /// Declares that this endpoint will be using queue with address <paramref name="address"/> for sending.
        /// </summary>
        /// <param name="address">The address of the queue.</param>
        public void BindSending(string address)
        {
            sendingAddresses.Add(address);
        }

        /// <summary>
        /// Returns the collection of all transport addresses of queues this endpoint is receiving from.
        /// </summary>
        public IEnumerable<string> ReceivingAddresses
        {
            get { return receiveAddresses; }
        }

        /// <summary>
        /// Returns the collection of all transport addresses of queues this endpoint is sending to.
        /// </summary>
        public IEnumerable<string> SendingAddresses
        {
            get { return sendingAddresses; }
        }
    }
}