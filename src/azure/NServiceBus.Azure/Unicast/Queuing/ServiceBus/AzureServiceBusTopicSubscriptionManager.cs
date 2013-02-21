using System;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Transport;
using NServiceBus.Unicast.Transport.Transactional;

namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    public class AzureServiceBusTopicSubscriptionManager : IManageSubscriptions
    {
        /// <summary>
        /// 
        /// </summary>
        public NamespaceManager NamespaceClient { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="original"></param>
        public void Subscribe(Type eventType, Address original)
        {
            var publisherAddress = Address.Parse(original.Queue + ".events");
            var subscriptionname = Configure.EndpointName + "." + eventType.Name;

            if (!NamespaceClient.SubscriptionExists(publisherAddress.Queue, subscriptionname))
            {
                NamespaceClient.CreateSubscription(publisherAddress.Queue, subscriptionname);
            }

            // how to make the correct strategy listen to this subscription

            var theBus = Configure.Instance.Builder.Build<UnicastBus>();

            var transport = theBus.Transport as TransactionalTransport;

            if (transport == null) return;

            var strategy = transport.Receiver as AzureServiceBusDequeueStrategy;

            if (strategy == null) return;
            
            var notifier = Configure.Instance.Builder.Build<AzureServiceBusTopicNotifier>();
            notifier.EventType = eventType;
            strategy.TrackNotifier(publisherAddress, notifier);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="original"></param>
        public void Unsubscribe(Type eventType, Address original)
        {
            var publisherAddress = Address.Parse(original.Queue + ".events");
            var subscriptionname = Configure.EndpointName + "." + eventType.Name;

            if (NamespaceClient.SubscriptionExists(publisherAddress.Queue, subscriptionname))
            {
                NamespaceClient.DeleteSubscription(publisherAddress.Queue, subscriptionname);
            }

            // unhook the listener
        }
    }
}