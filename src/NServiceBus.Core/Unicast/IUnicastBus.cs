namespace NServiceBus.Unicast
{
    using System;
    using Subscriptions;

    /// <summary>
    /// Extension of the IBus interface for working with a distributor.
    /// </summary>
    public interface IUnicastBus : IStartableBus
    {
        /// <summary>
        /// Event raised by the Publish method when no subscribers are
        /// registered for the message being published.
        /// </summary>
        event EventHandler<MessageEventArgs> NoSubscribersForMessage;

        /// <summary>
        /// Event raised when a client has been subscribed to a message type.
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0", Replacement = "MessageDrivenSubscriptionManager.ClientSubscribed")]
        event EventHandler<SubscriptionEventArgs> ClientSubscribed;

        /// <summary>
        /// Event raised when the bus sends multiple messages across the wire.
        /// </summary>
        event EventHandler<MessagesEventArgs> MessagesSent;

        /// <summary>
        /// Clears any existing timeouts for the given saga
        /// </summary>
        /// <param name="sagaId"></param>
        [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0", Replacement = "IDeferMessages.ClearDeferredMessages")]
        void ClearTimeoutsFor(Guid sagaId);
    }
}