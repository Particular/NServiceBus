namespace NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;

    /// <summary>
    /// Defines storage for subscriptions.
    /// </summary>
    public interface ISubscriptionStorage
    {
        /// <summary>
        /// Subscribes the given client to messages of a given type.
        /// </summary>
        Task Subscribe(Subscriber subscriber, MessageType messageType, ContextBag context);

        /// <summary>
        /// Unsubscribes the given client from messages of given type.
        /// </summary>
        Task Unsubscribe(Subscriber subscriber, MessageType messageType, ContextBag context);

        /// <summary>
        /// Returns a list of addresses for subscribers currently subscribed to the given message type.
        /// </summary>
        Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context);
    }
}