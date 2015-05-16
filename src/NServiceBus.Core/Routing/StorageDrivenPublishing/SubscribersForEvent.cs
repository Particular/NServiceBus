namespace NServiceBus.Routing.StorageDrivenPublishing
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Information available on the pipeline about the subscribers for a given message
    /// </summary>
    public class SubscribersForEvent
    {
        /// <summary>
        /// The list of subscribers for this event
        /// </summary>
        public IEnumerable<string> Subscribers { get; private set; }

        /// <summary>
        /// The event type
        /// </summary>
        public Type EventType { get; private set; }

        /// <summary>
        /// Ctor 
        /// </summary>
        public SubscribersForEvent(List<string> subscribers, Type eventType)
        {
            Subscribers = subscribers;
            EventType = eventType;
        }
    }
}