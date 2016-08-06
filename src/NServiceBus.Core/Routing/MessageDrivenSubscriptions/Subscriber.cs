namespace NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions
{
    using NServiceBus.Routing;

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
        public Subscriber(string transportAddress, EndpointName endpoint)
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
        public EndpointName Endpoint { get; }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <returns>
        /// true if the specified object is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object.</param>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj is Subscriber && Equals((Subscriber)obj);
        }

        bool Equals(Subscriber obj) => string.Equals(TransportAddress, obj.TransportAddress) && Equals(Endpoint, obj.Endpoint);

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        /// A hash code for the current object.
        /// </returns>
        public override int GetHashCode() => (TransportAddress + Endpoint).GetHashCode();

        /// <summary>
        /// Checks for equality.
        /// </summary>
        public static bool operator ==(Subscriber left, Subscriber right) => Equals(left, right);

        /// <summary>
        /// Checks for inequality.
        /// </summary>
        public static bool operator !=(Subscriber left, Subscriber right) => !Equals(left, right);
    }
}