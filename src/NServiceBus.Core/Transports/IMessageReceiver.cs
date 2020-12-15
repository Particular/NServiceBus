using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Transport;

namespace NServiceBus.Transports
{
    /// <summary>
    /// Allows the transport to push messages to the core.
    /// </summary>
    public interface IMessageReceiver
    {
        /// <summary>
        /// Starts pushing messages.
        /// </summary>
        Task StartReceive(PushRuntimeSettings limitations, Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<ErrorHandleResult>> onError, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops pushing messages.
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