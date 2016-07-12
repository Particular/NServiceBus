namespace NServiceBus.Transport
{
    using System.Collections.Generic;

    /// <summary>
    /// Contains information about queues this endpoint is using.
    /// </summary>
    public class QueueBindings
    {
        /// <summary>
        /// Returns the collection of all transport addresses of queues this endpoint is receiving from.
        /// </summary>
        public IReadOnlyCollection<string> ReceivingAddresses => receiveAddresses;

        /// <summary>
        /// Returns the collection of all transport addresses of queues this endpoint is sending to.
        /// </summary>
        public IReadOnlyCollection<string> SendingAddresses => sendingAddresses;

        /// <summary>
        /// Declares that this endpoint will be using queue with address <paramref name="address" /> for receiving.
        /// </summary>
        /// <param name="address">The address of the queue.</param>
        public void BindReceiving(string address)
        {
            receiveAddresses.Add(address);
        }

        /// <summary>
        /// Declares that this endpoint will be using queue with address <paramref name="transportAddress" /> for sending.
        /// </summary>
        /// <param name="transportAddress">The address of the queue.</param>
        public void BindSending(string transportAddress)
        {
            sendingAddresses.Add(transportAddress);
        }

        List<string> receiveAddresses = new List<string>();
        List<string> sendingAddresses = new List<string>();
    }
}