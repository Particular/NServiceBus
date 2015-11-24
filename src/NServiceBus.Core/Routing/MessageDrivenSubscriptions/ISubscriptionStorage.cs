namespace NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;

    /// <summary>
    /// Defines storage for subscriptions.
    /// </summary>
    public interface ISubscriptionStorage
    {
        /// <summary>
        /// Subscribes the given client to messages of the given types.
        /// </summary>
        Task Subscribe(Subscriber subscriber, IReadOnlyCollection<MessageType> messageTypes, ContextBag context);

        /// <summary>
        /// Unsubscribes the given client from messages of the given types.
        /// </summary>
        Task Unsubscribe(Subscriber subscriber, IReadOnlyCollection<MessageType> messageTypes, ContextBag context);

        /// <summary>
        /// Returns a list of addresses for subscribers currently subscribed to the given message type.
        /// </summary>
        Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IReadOnlyCollection<MessageType> messageTypes, ContextBag context);
    }
}