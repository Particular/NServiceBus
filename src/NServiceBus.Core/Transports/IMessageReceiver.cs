using System.Threading;

namespace NServiceBus.Transport
{
    using System;
    using System.Threading.Tasks;

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
        IManageSubscriptions Subscriptions { get; }

        /// <summary>
        /// 
        /// </summary>
        string Id { get; }
    }
}