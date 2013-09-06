namespace NServiceBus.Unicast.Publishing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using IdGeneration;
    using Subscriptions;
    using Subscriptions.MessageDrivenSubscriptions;
    using Transports;

    /// <summary>
    /// Published messages based on whats registered in the given subscription storage
    /// </summary>
    public class StorageDrivenPublisher:IPublishMessages
    {
        /// <summary>
        /// Subscription storge containing information about events and their subscribers
        /// </summary>
        public ISubscriptionStorage SubscriptionStorage { get; set; }

        /// <summary>
        /// The message sender to use when sending the events to the different publishers
        /// </summary>
        public ISendMessages MessageSender{ get; set; }
      
      
        /// <summary>
        /// Pubvlishes the given message to all subscribers
        /// </summary>
        /// <param name="message"></param>
        /// <param name="eventTypes"></param>
        /// <returns></returns>
        public bool Publish(TransportMessage message, IEnumerable<Type> eventTypes)
        {
            if (SubscriptionStorage == null)
                throw new InvalidOperationException("Cannot publish on this endpoint - no subscription storage has been configured. Add either 'MsmqSubscriptionStorage()' or 'DbSubscriptionStorage()' after 'NServiceBus.Configure.With()'.");

            var subscribers = Enumerable.ToList<Address>(SubscriptionStorage.GetSubscriberAddressesForMessage(eventTypes.Select(t => new MessageType(t))));

            if (!subscribers.Any())
                return false;

            foreach (var subscriber in subscribers)
            {
                //this is unicast so we give the message a unique ID
                message.ChangeMessageId(CombGuid.Generate().ToString());

                MessageSender.Send(message,subscriber);
            }

            return true;
        }
    }
}