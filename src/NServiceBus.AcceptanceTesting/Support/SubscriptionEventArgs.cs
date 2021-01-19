namespace NServiceBus.AcceptanceTesting
{
    public class SubscriptionEventArgs
    {
        /// <summary>
        /// The name of the subscriber endpoint.
        /// </summary>
        public string SubscriberEndpoint { get; set; }

        /// <summary>
        /// The address of the subscriber.
        /// </summary>
        public string SubscriberReturnAddress { get; set; }

        /// <summary>
        /// The type of message the client subscribed to.
        /// </summary>
        public string MessageType { get; set; }
    }
}