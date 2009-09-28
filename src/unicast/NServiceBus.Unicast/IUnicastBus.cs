using NServiceBus.Messages;
using System;

namespace NServiceBus.Unicast
{
    /// <summary>
    /// Extension of the IBus interface for working with a distributor.
    /// </summary>
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

        /// <summary>
        /// Instructs the bus to not send a <see cref="ReadyMessage"/>
        /// at the end of processing the current message on the specific thread
        /// on which it was called.
        /// </summary>
        void SkipSendingReadyMessageOnce();

        /// <summary>
        /// Event raised by the Publish method when no subscribers are
        /// registered for the message being published.
        /// </summary>
        event EventHandler<MessageEventArgs> NoSubscribersForMessage;

        /// <summary>
        /// Event raised when a client has been subscribed to a message type.
        /// </summary>
        event EventHandler<SubscriptionEventArgs> ClientSubscribed;
    }
}