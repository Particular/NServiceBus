namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using Routing;
    using Transports;
    using Unicast.Messages;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    class UnicastPublishRouter : UnicastRouter
    {
        public UnicastPublishRouter(string name, MessageMetadataRegistry messageMetadataRegistry, ISubscriptionStorage subscriptionStorage, EndpointInstances endpointInstances, TransportAddresses physicalAddresses, DistributionPolicy distributionPolicy, List<Type> allMessageTypes) 
            : base(name, messageMetadataRegistry, endpointInstances, physicalAddresses, distributionPolicy, allMessageTypes)
        {
            this.subscriptionStorage = subscriptionStorage;
        }

        protected override Task<IEnumerable<IUnicastRoute>> GetDestinationsFor(List<Type> messageTypeHierarchy)
        {
            return QuerySubscriptionStore(messageTypeHierarchy, new ContextBag());
        }

        async Task<IEnumerable<IUnicastRoute>> QuerySubscriptionStore(List<Type> types, ContextBag context)
        {
            var messageTypes = types.Select(t => new MessageType(t));
            var subscribers = await subscriptionStorage.GetSubscriberAddressesForMessage(messageTypes, context).ConfigureAwait(false);
            return subscribers.Select(s => new SubscriberDestination(s));
        }

        ISubscriptionStorage subscriptionStorage;

        class SubscriberDestination : IUnicastRoute
        {
            public SubscriberDestination(Subscriber subscriber)
            {
                if (subscriber.Endpoint != null)
                {
                    target = UnicastRoutingTarget.ToAnonymousInstance(subscriber.Endpoint, subscriber.TransportAddress);
                }
                else
                {
                    target = UnicastRoutingTarget.ToTransportAddress(subscriber.TransportAddress);
                }
            }

            public Task<IEnumerable<UnicastRoutingTarget>> Resolve(Func<EndpointName, Task<IEnumerable<EndpointInstance>>> instanceResolver)
            {
                return Task.FromResult(EnumerableEx.Single(target));
            }

            UnicastRoutingTarget target;
        }
    }
}