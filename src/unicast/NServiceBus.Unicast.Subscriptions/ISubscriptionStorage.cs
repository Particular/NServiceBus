using System.Collections.Generic;
using NServiceBus.Unicast.Transport;
using System;

namespace NServiceBus.Unicast.Subscriptions
{
	/// <summary>
	/// Defines storage for subscriptions
	/// </summary>
    public interface ISubscriptionStorage
    {
        /// <summary>
        /// Check to see if <see cref="msg"/> is a <see cref="SubscriptionMessage"/>.
        /// If so, performs the relevant subscribe/unsubscribe.
        /// </summary>
        /// <param name="msg">The message received in the bus.</param>
	    void HandleSubscriptionMessage(TransportMessage msg);

        /// <summary>
        /// Returns a list of addresses of subscribers that previously requested to be notified
        /// of messages of the same type as <see cref="messageType"/>.
        /// </summary>
        /// <param name="messageType">The logical message type that the bus wishes to publish.</param>
        /// <returns>List of addresses of subscribers.</returns>
        IList<string> GetSubscribersForMessage(Type messageType);

        /// <summary>
        /// Notifies the subscription storage that now is the time to perform
        /// any initialization work
        /// </summary>
	    void Init();
    }
}
