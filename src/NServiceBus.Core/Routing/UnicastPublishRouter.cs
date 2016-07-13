namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using Routing;
    using Transport;
    using Unicast.Messages;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    class UnicastPublishRouter : UnicastRouter
    {
        public UnicastPublishRouter(MessageMetadataRegistry messageMetadataRegistry, ISubscriptionStorage subscriptionStorage, EndpointInstances endpointInstances, TransportAddresses physicalAddresses) : base(messageMetadataRegistry, endpointInstances, physicalAddresses)
        {
            this.subscriptionStorage = subscriptionStorage;
        }

        protected override async Task<List<UnicastRoute>> GetDestinations(ContextBag contextBag, Type[] typesToRoute)
        {
            var messageTypes = typesToRoute.Select(t => new MessageType(t));
            var subscribers = await subscriptionStorage.GetSubscriberAddressesForMessage(messageTypes, contextBag).ConfigureAwait(false);
            return subscribers.Select(subscriber => subscriber.Endpoint != null
                ? UnicastRoute.CreateFromEndpointInstance(new EndpointInstance(subscriber.Endpoint, address: subscriber.TransportAddress))
                : UnicastRoute.CreateFromPhysicalAddress(subscriber.TransportAddress)).ToList();
        }

        ISubscriptionStorage subscriptionStorage;
    }
}