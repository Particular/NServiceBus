namespace NServiceBus.Unicast.Subscriptions
{
    using System;

    /// <summary>
    /// Contains which client subscribed to which message
    /// </summary>
    public class SubscriptionEventArgs : EventArgs
    {
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
