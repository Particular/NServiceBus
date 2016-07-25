namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using Routing;
    using Unicast.Messages;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    class UnicastPublishRouter : UnicastRouter
    {
        public UnicastPublishRouter(MessageMetadataRegistry messageMetadataRegistry, ISubscriptionStorage subscriptionStorage, EndpointInstances endpointInstances, Func<EndpointInstance, string> transportAddressTranslation) 
            : base(messageMetadataRegistry, endpointInstances, transportAddressTranslation)
        {
            this.subscriptionStorage = subscriptionStorage;
        }

        protected override async Task<IEnumerable<IUnicastRoute>> GetDestinations(ContextBag contextBag, Type[] typesToRoute)
        {
            var messageTypes = typesToRoute.Select(t => new MessageType(t));
            var subscribers = await subscriptionStorage.GetSubscriberAddressesForMessage(messageTypes, contextBag).ConfigureAwait(false);
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

            public Task<IEnumerable<UnicastRoutingTarget>> Resolve(Func<string, Task<IEnumerable<EndpointInstance>>> instanceResolver)
            {
                return Task.FromResult(EnumerableEx.Single(target));
            }

            UnicastRoutingTarget target;
        }
    }
}