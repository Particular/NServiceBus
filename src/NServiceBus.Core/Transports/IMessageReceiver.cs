using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Transport;
using NServiceBus.Unicast.Messages;

namespace NServiceBus.Transports
{
    /// <summary>
    /// Allows the transport to push messages to the core.
    /// </summary>
    public interface IMessageReceiver
    {
        /// <summary>
        /// Initializes the receiver.
        /// </summary>
        Task Initialize(PushRuntimeSettings limitations, Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<ErrorHandleResult>> onError, IReadOnlyCollection<MessageMetadata> events, CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts receiving messages from the input queue.
        /// </summary>
        Task StartReceive(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops receiving messages.
        /// </summary>
        Task StopReceive(CancellationToken cancellationToken = default);

        /// <summary>
        /// 
        /// </summary>
        ISubscriptionManager Subscriptions { get; }

        /// <summary>
        /// 
        /// </summary>
        string Id { get; }
    }
}