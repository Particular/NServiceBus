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
        /// Check to see if the given message is a <see cref="SubscriptionMessage"/>.
        /// If so, performs the relevant subscribe/unsubscribe.
        /// </summary>
        /// <param name="msg">The message received in the bus.</param>
	    void HandleSubscriptionMessage(TransportMessage msg);

        /// <summary>
        /// Returns a list of addresses of subscribers that previously requested to be notified
        /// of messages of the same type as the given message type.
        /// </summary>
        /// <param name="messageType">The logical message type that the bus wishes to publish.</param>
        /// <returns>List of addresses of subscribers.</returns>
        IList<string> GetSubscribersForMessage(Type messageType);

        /// <summary>
        /// Notifies the subscription storage that now is the time to perform
        /// any initialization work
        /// </summary>
        /// <param name="messageTypes">The logical message types that the bus will be publishing.</param>
        void Init(IList<Type> messageTypes);
    }
}
