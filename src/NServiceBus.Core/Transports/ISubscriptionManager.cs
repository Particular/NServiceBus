namespace NServiceBus.Transport
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Unicast.Messages;

    /// <summary>
    /// Implemented by transports to provide pub/sub capabilities.
    /// </summary>
    public interface ISubscriptionManager
    {
        /// <summary>
        /// Subscribes to all provided events.
        /// </summary>
        Task SubscribeAll(MessageMetadata[] eventTypes, ContextBag context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Unsubscribes from the given event.
        /// </summary>
        Task Unsubscribe(MessageMetadata eventType, ContextBag context, CancellationToken cancellationToken = default);
    }
}