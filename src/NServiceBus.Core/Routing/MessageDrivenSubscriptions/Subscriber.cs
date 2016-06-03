namespace NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions
{
    /// <summary>
    /// Represents a subscriber in message-driven subscriptions.
    /// </summary>
    public class Subscriber
    {
        /// <summary>
        /// Creates a new subscriber.
        /// </summary>
        /// <param name="transportAddress">Transport address.</param>
        /// <param name="endpoint">Endpoint name (optional).</param>
        public Subscriber(string transportAddress, string endpoint)
        {
            TransportAddress = transportAddress;
            Endpoint = endpoint;
        }

        /// <summary>
        /// The transport address of the subscriber.
        /// </summary>
        public string TransportAddress { get; }

        /// <summary>
        /// The endpoint name of the subscriber or null if unknown.
        /// </summary>
        public string Endpoint { get; }
    }
}