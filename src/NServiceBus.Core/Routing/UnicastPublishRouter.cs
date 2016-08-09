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
        public UnicastPublishRouter(MessageMetadataRegistry messageMetadataRegistry, ISubscriptionStorage subscriptionStorage, Func<EndpointInstance, string> transportAddressTranslation)
        {
            this.messageMetadataRegistry = messageMetadataRegistry;
            this.subscriptionStorage = subscriptionStorage;
            this.transportAddressTranslation = transportAddressTranslation;
        }

        public async Task<IEnumerable<UnicastRoutingStrategy>> Route(Type messageType, IDistributionPolicy distributionPolicy, ContextBag contextBag)
        {
            var typesToRoute = messageMetadataRegistry.GetMessageMetadata(messageType).MessageHierarchy;

            var routes = await GetDestinations(contextBag, typesToRoute).ConfigureAwait(false);

            var selectedDestinations = SelectDestinationsForEachEndpoint(distributionPolicy, routes);

            return selectedDestinations
                .Select(destination => destination.Resolve(x => transportAddressTranslation(x)))
                .Distinct() //Make sure we are sending only one to each transport destination. Might happen when there are multiple routing information sources.
                .Select(destination => new UnicastRoutingStrategy(destination));
        }

        static IEnumerable<UnicastRoutingTarget> SelectDestinationsForEachEndpoint(IDistributionPolicy distributionPolicy, IEnumerable<UnicastRoutingTarget> destinations)
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
                    var destinationForEndpoint = distributionPolicy.GetDistributionStrategy(@group.Key).SelectDestination(@group.ToArray());
                    if (destinationForEndpoint != null)
                    {
                        yield return destinationForEndpoint;
                    }
                }
            }
        }

        async Task<IEnumerable<UnicastRoutingTarget>> GetDestinations(ContextBag contextBag, Type[] typesToRoute)
        {
            var messageTypes = typesToRoute.Select(t => new MessageType(t));
            var subscribers = await subscriptionStorage.GetSubscriberAddressesForMessage(messageTypes, contextBag).ConfigureAwait(false);
            return subscribers.Select(s => CreateRoutingTarget(s));
        }

        static UnicastRoutingTarget CreateRoutingTarget(Subscriber subscriber)
        {
            return subscriber.Endpoint != null
                ? UnicastRoutingTarget.ToAnonymousInstance(subscriber.Endpoint, subscriber.TransportAddress)
                : UnicastRoutingTarget.ToTransportAddress(subscriber.TransportAddress);
        }

        Func<EndpointInstance, string> transportAddressTranslation;
        MessageMetadataRegistry messageMetadataRegistry;
        ISubscriptionStorage subscriptionStorage;
    }
}