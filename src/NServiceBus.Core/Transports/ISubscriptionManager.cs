﻿using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Unicast.Messages;

namespace NServiceBus.Transport
{
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