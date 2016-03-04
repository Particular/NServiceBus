namespace NServiceBus
{
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

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
        public Subscriber Subscriber { get; set; }
    }
}