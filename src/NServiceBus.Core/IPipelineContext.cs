namespace NServiceBus
{
    using System.Threading.Tasks;
    using Extensibility;

    /// <summary>
    /// The context for the current message handling pipeline.
    /// </summary>
    public interface IPipelineContext : IExtendable
    {
        /// <summary>
        /// Sends the provided message.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="options">The options for the send.</param>
        Task Send(object message, SendOptions options);

        /// <summary>
        /// Publish the message to subscribers.
        /// </summary>
        /// <param name="message">The message to publish.</param>
        /// <param name="options">The options for the publish.</param>
        Task Publish(object message, PublishOptions options);
    }
}