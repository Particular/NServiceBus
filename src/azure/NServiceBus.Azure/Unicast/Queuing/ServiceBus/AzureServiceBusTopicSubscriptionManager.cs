using System;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using NServiceBus.Unicast.Subscriptions;

namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    public class AzureServiceBusTopicSubscriptionManager : IManageSubscriptions
    {
        public MessagingFactory Factory { get; set; }
        public NamespaceManager NamespaceClient { get; set; }

        public void Subscribe(Type eventType, Address publisherAddress)
        {
            if (!NamespaceClient.SubscriptionExists(publisherAddress.Queue + ".events", Configure.EndpointName))
            {
                NamespaceClient.CreateSubscription(publisherAddress.Queue + ".events", Configure.EndpointName);
            }

            // hook up  the listener
        }

        public void Unsubscribe(Type eventType, Address publisherAddress)
        {
            if (NamespaceClient.SubscriptionExists(publisherAddress.Queue + ".events", Configure.EndpointName))
            {
                NamespaceClient.DeleteSubscription(publisherAddress.Queue + ".events", Configure.EndpointName);
            }

            // unhook the listener
        }
    }
}