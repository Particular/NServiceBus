namespace NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
	/// Defines storage for subscriptions.
	/// </summary>
    public interface ISubscriptionStorage
    {
        /// <summary>
        /// Subscribes the given client address to messages of the given types.
        /// </summary>
        Task Subscribe(string client, IEnumerable<MessageType> messageTypes, SubscriptionStorageOptions options);

        /// <summary>
        /// Unsubscribes the given client address from messages of the given types.
        /// </summary>
        Task Unsubscribe(string client, IEnumerable<MessageType> messageTypes, SubscriptionStorageOptions options);

        /// <summary>
        /// Returns a list of addresses for subscribers currently subscribed to the given message type.
        /// </summary>
        Task<IEnumerable<string>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, SubscriptionStorageOptions options);
    }
}
