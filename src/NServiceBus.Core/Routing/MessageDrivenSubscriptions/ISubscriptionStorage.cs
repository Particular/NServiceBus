namespace NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions
{
    using System.Collections.Generic;

    /// <summary>
	/// Defines storage for subscriptions.
	/// </summary>
    public interface ISubscriptionStorage
    {
        /// <summary>
        /// Subscribes the given client address to messages of the given types.
        /// </summary>
        void Subscribe(string client, IEnumerable<MessageType> messageTypes, SubscriptionStorageOptions options);

        /// <summary>
        /// Unsubscribes the given client address from messages of the given types.
        /// </summary>
        void Unsubscribe(string client, IEnumerable<MessageType> messageTypes, SubscriptionStorageOptions options);
    }
}
