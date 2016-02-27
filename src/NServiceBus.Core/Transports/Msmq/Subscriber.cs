namespace NServiceBus.Transports.Msmq
{
    /// <summary>
    /// Represents a subscribed endpoint.
    /// </summary>
    public class Subscriber
    {
        /// <summary>
        /// Name of the subscribed endpoint.
        /// </summary>
        public string Endpoint { get; }
        /// <summary>
        /// Transport address to which to send messages.
        /// </summary>
        public string TransportAddress { get; }

        /// <summary>
        /// Creates new subscriber.
        /// </summary>
        /// <param name="endpoint">Name of the endpoint.</param>
        /// <param name="transportAddress">Transport address.</param>
        public Subscriber(string endpoint, string transportAddress)
        {
            Endpoint = endpoint;
            TransportAddress = transportAddress;
        }
    }
}