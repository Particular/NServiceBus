namespace NServiceBus.Transport
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Allows the transport to push messages to the core.
    /// </summary>
    public interface IPushMessages
    {
        /// <summary>
        /// Prepare the message pump to be started.
        /// </summary>
        /// <param name="onMessage">Called when there is a message available for processing.</param>
        /// <param name="onError">Called when there is a message has failed mprocessing.</param>
        /// <param name="criticalError">Called when there is a critical error in the message pump.</param>
        /// <param name="settings">Runtime settings for the message pump.</param>
        Task Init(Func<MessageContext, Task> onMessage,
            Func<ErrorContext, Task<ErrorHandleResult>> onError,
            CriticalError criticalError,
            PushSettings settings);

        /// <summary>
        /// Starts pushing messages.
        /// </summary>
        void Start(PushRuntimeSettings limitations);

        /// <summary>
        /// Stops pushing messages.
        /// </summary>
        Task Stop();
    }
}