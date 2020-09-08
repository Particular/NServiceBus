namespace NServiceBus.Transport
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Allows the transport to push messages to the core.
    /// </summary>
    public interface IPushMessages
    {
        /// <summary>
        /// Prepares the message pump to be started.
        /// </summary>
        /// <param name="onMessage">Called when there is a message available for processing.</param>
        /// <param name="onError">Called when there is a message that has failed processing.</param>
        /// <param name="criticalError">Called when there is a critical error in the message pump.</param>
        /// <param name="settings">Runtime settings for the message pump.</param>
        /// <param name="cancellationToken"></param>
        Task Init(Func<MessageContext, CancellationToken, Task> onMessage,
            Func<ErrorContext, CancellationToken, Task<ErrorHandleResult>> onError,
            CriticalError criticalError,
            PushSettings settings,
            CancellationToken cancellationToken
            );

        /// <summary>
        /// Starts pushing messages.
        /// </summary>
        void Start(PushRuntimeSettings limitations, CancellationToken cancellationToken);

        /// <summary>
        /// Stops pushing messages.
        /// </summary>
        Task Stop(CancellationToken cancellationToken);
    }
}