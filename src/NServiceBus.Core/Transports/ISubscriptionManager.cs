namespace NServiceBus.Transport
{
    using System.Threading.Tasks;
    using Extensibility;
    using Unicast.Messages;

    /// <summary>
    /// Implemented by transports to provide pub/sub capabilities.
    /// </summary>
    public interface ISubscriptionManager
    {
        /// <summary>
        /// Subscribes to the given event.
        /// </summary>
        Task Subscribe(MessageMetadata eventType, ContextBag context);

        /// <summary>
        /// Unsubscribes from the given event.
        /// </summary>
        Task Unsubscribe(MessageMetadata eventType, ContextBag context);
    }
}