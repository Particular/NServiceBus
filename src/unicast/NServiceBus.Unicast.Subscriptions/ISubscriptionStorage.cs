using System.Collections.Generic;
using System;

namespace NServiceBus.Unicast.Subscriptions
{
	/// <summary>
	/// Defines storage for subscriptions
	/// </summary>
    public interface ISubscriptionStorage
    {
        /// <summary>
        /// Subscribes the given client address to messages of the given messageType.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="messageType"></param>
	    void Subscribe(string client, string messageType);

        /// <summary>
        /// Unsubscribes the given client address from messages of the given messageType.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="messageType"></param>
	    void Unsubscribe(string client, string messageType);

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
