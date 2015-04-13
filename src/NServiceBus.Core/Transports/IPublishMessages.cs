namespace NServiceBus.Transports
{
    using Unicast;

    /// <summary>
    /// Requests a message to be published
    /// </summary>
    public interface IPublishMessages
    {
        /// <summary>
        /// Publishes the given messages to all known subscribers
        /// </summary>
        void Publish(OutgoingMessage message, PublishMessageOptions publishOptions);
    }
}