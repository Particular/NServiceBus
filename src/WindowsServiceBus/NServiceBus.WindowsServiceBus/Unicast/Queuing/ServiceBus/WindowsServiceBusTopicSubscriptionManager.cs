using System;
using Microsoft.ServiceBus;
using NServiceBus.Unicast.Transport.Transactional;

namespace NServiceBus.Unicast.Queuing.Windows.ServiceBus
{
    using Transport;
    using Transports;

	public class WindowsServiceBusTopicSubscriptionManager : IManageSubscriptions
    {
        /// <summary>
        /// 
        /// </summary>
        public NamespaceManager NamespaceClient { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ICreateSubscriptionClients ClientCreator { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="original"></param>
        public void Subscribe(Type eventType, Address original)
        {
            var publisherAddress = Address.Parse(original.Queue + ".events");
            var subscriptionname = Configure.EndpointName + "." + eventType.Name;

            ClientCreator.Create(eventType, publisherAddress.Queue, subscriptionname);

            // how to make the correct strategy listen to this subscription

            var theBus = Configure.Instance.Builder.Build<UnicastBus>();

            var transport = theBus.Transport as TransportReceiver;

            if (transport == null) return;

			var strategy = transport.Receiver as WindowsServiceBusDequeueStrategy;

            if (strategy == null) return;

			var notifier = Configure.Instance.Builder.Build<WindowsServiceBusTopicNotifier>();
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