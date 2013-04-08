namespace NServiceBus.Persistence.Msmq.SubscriptionStorage
{
    using System;
    using Unicast.Subscriptions;

    /// <summary>
    /// Describes an entry in the list of subscriptions.
    /// </summary>
    [Serializable]
    public class Entry
    {
        /// <summary>
        /// Gets the message type for the subscription entry.
        /// </summary>
        public MessageType MessageType { get; set; }

        /// <summary>
        /// Gets the subscription request message.
        /// </summary>
        public Address Subscriber { get; set; }
    }
}
