namespace NServiceBus.AcceptanceTests.PubSub
{
    public class SubscriptionEventArgs
    {
        /// <summary>
        /// The address of the subscriber.
        /// </summary>
        public Address SubscriberReturnAddress { get; set; }

        /// <summary>
        /// The type of message the client subscribed to.
        /// </summary>
        public string MessageType { get; set; }
    }
}