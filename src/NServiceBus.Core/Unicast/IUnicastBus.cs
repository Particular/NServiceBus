namespace NServiceBus.Unicast
{
    using System;

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

    }
}