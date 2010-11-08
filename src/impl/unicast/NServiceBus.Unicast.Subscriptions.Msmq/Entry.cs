using System;

namespace NServiceBus.Unicast.Subscriptions.Msmq
{
    /// <summary>
    /// Describes an entry in the list of subscriptions.
    /// </summary>
    [Serializable]
    public class Entry
    {
        /// <summary>
        /// Gets the message type for the subscription entry.
        /// </summary>
        public string MessageType { get; set; }

        /// <summary>
        /// Gets the subscription request message.
        /// </summary>
        public string Subscriber { get; set; }
    }
}
