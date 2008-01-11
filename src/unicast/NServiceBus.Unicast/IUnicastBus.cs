using NServiceBus.Messages;

namespace NServiceBus.Unicast
{
    public interface IUnicastBus : IBus
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
    }
}
