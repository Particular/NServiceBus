using System;

namespace NServiceBus.Unicast
{
    /// <summary>
    /// Contains which client subscribed to which message
    /// </summary>
    public class SubscriptionEventArgs : EventArgs
    {
        /// <summary>
        /// The address of the subscriber.
        /// </summary>
        [ObsoleteEx(Replacement = "SubscriberReturnAddress", TreatAsErrorFromVersion = "3.0", RemoveInVersion = "4.0")]
        public string SubscriberAddress { get; set; }

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
