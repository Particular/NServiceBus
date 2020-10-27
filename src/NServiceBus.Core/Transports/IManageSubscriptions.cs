using System.Threading;

namespace NServiceBus.Transport
{
    using System;
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
        Task Subscribe(Type eventType, ContextBag context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Unsubscribes from the given event.
        /// </summary>
        Task Unsubscribe(Type eventType, ContextBag context, CancellationToken cancellationToken = default);
    }
}