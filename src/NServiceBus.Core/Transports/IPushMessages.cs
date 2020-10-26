using System.Threading;

namespace NServiceBus.Transport
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Allows the transport to push messages to the core.
    /// </summary>
    public interface IPushMessages
    {
       ////TODO rename to StartReceive & StopReceive
       
       /// <summary>
        /// Starts pushing messages.
        /// </summary>
        Task Start(PushRuntimeSettings limitations, Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<ErrorHandleResult>> onError, CancellationToken cancellationToken);

        /// <summary>
        /// Stops pushing messages.
        /// </summary>
        Task Stop(CancellationToken cancellationToken);

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