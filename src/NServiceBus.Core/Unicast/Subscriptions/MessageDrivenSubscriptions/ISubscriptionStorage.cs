namespace NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions
{
    using System.Collections.Generic;

    /// <summary>
	/// Defines storage for subscriptions
	/// </summary>
    public interface ISubscriptionStorage
    {

        /// <summary>
        /// Subscribes the given client address to messages of the given types.
        /// </summary>
        void Subscribe(Address client, IEnumerable<MessageType> messageTypes);

        /// <summary>
        /// Unsubscribes the given client address from messages of the given types.
        /// </summary>
        void Unsubscribe(Address client, IEnumerable<MessageType> messageTypes);

        /// <summary>
        /// Returns a list of addresses of subscribers that previously requested to be notified
        /// of messages of the given message types.
        /// </summary>
        IEnumerable<Address> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes);

        /// <summary>
        /// Notifies the subscription storage that now is the time to perform
        /// any initialization work
        /// </summary>
        void Init();
    }
}
