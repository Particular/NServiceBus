namespace NServiceBus.Transports
{
    using System;

    /// <summary>
    /// Implemented by transports to provide pub/sub capabilities
    /// </summary>
    public interface IManageSubscriptions
    {
        /// <summary>
        /// Subscribes to the given event. For message driven transports like msmq and sqlserver the address of the publisher is needed as well
        /// </summary>
        /// <param name="eventType">The event type</param>
        /// <param name="publisherAddress">The publisher address if needed</param>
        void Subscribe(Type eventType, string publisherAddress);
        
        /// <summary>
        /// Unsubscribes from the given event. For message driven transports like msmq and sqlserver the address of the publisher is needed as well
        /// </summary>
        /// <param name="eventType">The event type</param>
        /// <param name="publisherAddress">The publisher address if needed</param>
        void Unsubscribe(Type eventType, string publisherAddress);
    }
}