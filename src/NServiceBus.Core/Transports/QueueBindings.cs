namespace NServiceBus.Transport
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Contains information about queues this endpoint is using.
    /// </summary>
    public partial class QueueBindings
    {
        /// <summary>
        /// Returns the collection of all transport addresses of queues this endpoint is sending to.
        /// </summary>
        public IReadOnlyCollection<string> SendingAddresses => sendingAddresses;

        /// <summary>
        /// Declares that this endpoint will be using queue with address <paramref name="transportAddress" /> for sending.
        /// </summary>
        /// <param name="transportAddress">The address of the queue.</param>
        public void BindSending(string transportAddress)
        {
            ArgumentException.ThrowIfNullOrEmpty(transportAddress);
            sendingAddresses.Add(transportAddress);
        }

        readonly List<string> sendingAddresses = new List<string>();
    }
}