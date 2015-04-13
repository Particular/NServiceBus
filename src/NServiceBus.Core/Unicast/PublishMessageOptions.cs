namespace NServiceBus.Unicast
{
    using System;

    /// <summary>
    /// Additional options that only applies when publishing messages
    /// </summary>
    public class PublishMessageOptions : DeliveryMessageOptions
    {
        readonly Type eventType;

        /// <summary>
        /// The type of event to publish.
        /// </summary>
        public Type EventType
        {
            get { return eventType; }
        }

        /// <summary>
        /// The event type is required for a publish.
        /// </summary>
        public PublishMessageOptions(Type eventType)
        {
            Guard.AgainstNull(eventType, "eventType");
            this.eventType = eventType;
        }
    }
}