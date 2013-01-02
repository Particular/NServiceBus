namespace NServiceBus.Transport.ActiveMQ
{
    using System;
    using System.Collections.Generic;

    using NServiceBus.Unicast.Subscriptions;

    public class ActiveMqSubscriptionStorage : ISubscriptionStorage
    {
        public void Subscribe(Address client, IEnumerable<MessageType> messageTypes)
        {
            throw new InvalidOperationException("Subscriptions are handled by ActiveMQ. They must never arrive on the publisher.");
        }

        public void Unsubscribe(Address client, IEnumerable<MessageType> messageTypes)
        {
            throw new InvalidOperationException("Subscriptions are handled by ActiveMQ. They must never arrive on the publisher.");
        }

        public IEnumerable<Address> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes)
        {
            // Returns just a local adress because Subscriptions are handled by AMQ. 
            // Therefor the only subscriber is the Queue.
            return new[] { Address.Local };
        }

        public void Init()
        {
        }
    }
}
