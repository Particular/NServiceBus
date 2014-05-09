namespace NServiceBus.Transports
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Requests a message to be published
    /// </summary>
    public interface IPublishMessages
    {
        /// <summary>
        /// Publishes the given messages to all known subscribers
        /// </summary>
        void Publish(TransportMessage message, IEnumerable<Type> eventTypes);
    }
}