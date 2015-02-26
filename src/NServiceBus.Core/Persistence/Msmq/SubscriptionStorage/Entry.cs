namespace NServiceBus.Persistence.Msmq.SubscriptionStorage
{
    using Unicast.Subscriptions;

    /// <summary>
    /// Describes an entry in the list of subscriptions.
    /// </summary>
    class Entry
    {
        /// <summary>
        /// Gets the message type for the subscription entry.
        /// </summary>
        public MessageType MessageType { get; set; }

        /// <summary>
        /// The subscriber address.
        /// </summary>
        public string Subscriber { get; set; }
    }
}
