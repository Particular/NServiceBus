namespace NServiceBus.Unicast
{
    using System;

    /// <summary>
    /// Additional options that only applies when publishing messages
    /// </summary>
    public class PublishOptions : DeliveryOptions
    {
        /// <summary>
        /// The type of event to publish
        /// </summary>
        public Type EventType { get; private set; }

        /// <summary>
        /// The event type is required for a publish
        /// </summary>
        /// <param name="eventType"></param>
        public PublishOptions(Type eventType)
        {
            EventType = eventType;
        }
    }
}