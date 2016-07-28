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

    class UnicastPublishRouter : IUnicastPublishRouter
    {
        public UnicastPublishRouter(MessageMetadataRegistry messageMetadataRegistry, ISubscriptionStorage subscriptionStorage, EndpointInstances endpointInstances, Func<EndpointInstance, string> transportAddressTranslation)
        {
            this.messageMetadataRegistry = messageMetadataRegistry;
            this.subscriptionStorage = subscriptionStorage;
            this.endpointInstances = endpointInstances;
            this.transportAddressTranslation = transportAddressTranslation;
        }

        public async Task<IEnumerable<UnicastRoutingStrategy>> Route(Type messageType, IDistributionPolicy distributionPolicy, ContextBag contextBag)
        {
            var typesToRoute = messageMetadataRegistry.GetMessageMetadata(messageType).MessageHierarchy;

            var routes = await GetDestinations(contextBag, typesToRoute).ConfigureAwait(false);
            var destinations = new HashSet<UnicastRoutingTarget>();
            foreach (var route in routes)
            {
                var routingTargets = await route.Resolve(endpoint => endpointInstances.FindInstances(endpoint)).ConfigureAwait(false);
                foreach (var routingTarget in routingTargets)
                {
                    destinations.Add(routingTarget);
                }
            }

            var selectedDestinations = SelectDestinationsForEachEndpoint(distributionPolicy, destinations);

            return selectedDestinations
                .Select(destination => destination.Resolve(x => transportAddressTranslation(x)))
                .Distinct() //Make sure we are sending only one to each transport destination. Might happen when there are multiple routing information sources.
                .Select(destination => new UnicastRoutingStrategy(destination));
        }

        static IEnumerable<UnicastRoutingTarget> SelectDestinationsForEachEndpoint(IDistributionPolicy distributionPolicy, HashSet<UnicastRoutingTarget> destinations)
        {
            var destinationsByEndpoint = destinations
                .GroupBy(d => d.Endpoint, d => d);

            foreach (var group in destinationsByEndpoint)
            {
                if (group.Key == null) //Routing targets that do not specify endpoint name
                {
                    //Send a message to each target as we have no idea which endpoint they represent
                    foreach (var destination in group)
                    {
                        yield return destination;
                    }
                }
                else
                {
                    //Use the distribution strategy to select subset of instances of a given endpoint
                    foreach (var destination in distributionPolicy.GetDistributionStrategy(group.Key).SelectDestination(group.ToArray()))
                    {
                        yield return destination;
                    }
                }
            }
        }

        async Task<IEnumerable<IUnicastRoute>> GetDestinations(ContextBag contextBag, Type[] typesToRoute)
        {
            var messageTypes = typesToRoute.Select(t => new MessageType(t));
            var subscribers = await subscriptionStorage.GetSubscriberAddressesForMessage(messageTypes, contextBag).ConfigureAwait(false);
            return subscribers.Select(s => new SubscriberDestination(s));
        }

        EndpointInstances endpointInstances;
        Func<EndpointInstance, string> transportAddressTranslation;
        MessageMetadataRegistry messageMetadataRegistry;
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