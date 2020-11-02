using System.Threading;
using NServiceBus.Unicast.Messages;

namespace NServiceBus.Transport
{
    using System.Threading.Tasks;
    using Extensibility;

    /// <summary>
    /// Implemented by transports to provide pub/sub capabilities.
    /// </summary>
    public interface IManageSubscriptions
    {
        /// <summary>
        /// Subscribes to the given event.
        /// </summary>
        Task Subscribe(MessageMetadata eventType, ContextBag context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Unsubscribes from the given event.
        /// </summary>
        Task Unsubscribe(MessageMetadata eventType, ContextBag context, CancellationToken cancellationToken = default);
    }
}