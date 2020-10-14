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
       ////TODO can we move PushRUntimeSettings to the receivesettings too?
       
       ////TODO Start should become async to handle init/setup work

        /// <summary>
        /// Starts pushing messages.
        /// </summary>
        void Start(PushRuntimeSettings limitations, Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<ErrorHandleResult>> onError);

        /// <summary>
        /// Stops pushing messages.
        /// </summary>
        Task Stop();

        /// <summary>
        /// 
        /// </summary>
        IManageSubscriptions Subscriptions { get; }
    }
}