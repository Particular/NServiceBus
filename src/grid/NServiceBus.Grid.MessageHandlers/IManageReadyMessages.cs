using NServiceBus.Grid.Messages;

namespace NServiceBus.Grid.MessageHandlers
{
    /// <summary>
    /// Manages ready messages
    /// </summary>
    public interface IManageReadyMessages
    {
        /// <summary>
        /// Instructs the bus to stop sending <see cref="ReadyMessage"/>s
        /// when it has a distributor configured.
        /// </summary>
        void StopSendingReadyMessages();

        /// <summary>
        /// Instructs the bus to continue sending <see cref="ReadyMessage"/>s
        /// when it has a distributor configured.
        /// </summary>
        void ContinueSendingReadyMessages();

        /// <summary>
        /// Instructs the bus to not send a <see cref="ReadyMessage"/>
        /// at the end of processing the current message on the specific thread
        /// on which it was called.
        /// </summary>
        void SkipSendingReadyMessageOnce();
    }
}
