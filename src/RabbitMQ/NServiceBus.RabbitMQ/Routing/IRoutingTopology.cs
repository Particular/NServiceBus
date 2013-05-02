namespace NServiceBus.Transports.RabbitMQ.Routing
{
    using System;
    using global::RabbitMQ.Client;

    /// <summary>
    /// Topology for routing messages on the transport
    /// </summary>
    public interface IRoutingTopology
    {
        /// <summary>
        /// Set up subscription for subscriber to the specified type
        /// </summary>
        /// <param name="channel">RabbitMQ channel to operate on</param>
        /// <param name="type">Type to handle with subscriber</param>
        /// <param name="subscriberName">Subscriber name</param>
        void SetupSubscription(IModel channel, Type type, string subscriberName);
        /// <summary>
        /// Stop subscription for subscriber to the specified type
        /// </summary>
        /// <param name="channel">RabbitMQ channel to operate on</param>
        /// <param name="type">Type to handle with subscriber</param>
        /// <param name="subscriberName">Subscriber name</param>
        void TeardownSubscription(IModel channel, Type type, string subscriberName);
        /// <summary>
        /// Publish message of the specified type
        /// </summary>
        /// <param name="channel">RabbitMQ channel to operate on</param>
        /// <param name="type">Type to handle with subscriber</param>
        /// <param name="message">Message to publish</param>
        /// <param name="properties">RabbitMQ properties of the message to publish</param>
        void Publish(IModel channel, Type type, TransportMessage message, IBasicProperties properties);
        /// <summary>
        /// Send message to the specified endpoint
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="address"></param>
        /// <param name="message"></param>
        /// <param name="properties"></param>
        void Send(IModel channel, Address address, TransportMessage message, IBasicProperties properties);
    }
}