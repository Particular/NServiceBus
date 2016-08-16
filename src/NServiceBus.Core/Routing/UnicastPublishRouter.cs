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
        public UnicastPublishRouter(MessageMetadataRegistry messageMetadataRegistry, ISubscriptionStorage subscriptionStorage)
        {
            this.messageMetadataRegistry = messageMetadataRegistry;
            this.subscriptionStorage = subscriptionStorage;
        }

        public async Task<IEnumerable<UnicastRoutingStrategy>> Route(Type messageType, IDistributionPolicy distributionPolicy, ContextBag contextBag)
        {
            var typesToRoute = messageMetadataRegistry.GetMessageMetadata(messageType).MessageHierarchy;

            var subscribers = await GetDestinations(contextBag, typesToRoute).ConfigureAwait(false);

            var selectedDestinations = SelectDestinationsForEachEndpoint(distributionPolicy, subscribers);

            return selectedDestinations
                .Distinct() //Make sure we are sending only one to each transport destination. Might happen when there are multiple routing information sources.
                .Select(destination => new UnicastRoutingStrategy(destination));
        }

        IEnumerable<string> SelectDestinationsForEachEndpoint(IDistributionPolicy distributionPolicy, IEnumerable<Subscriber> subscribers)
        {
            var destinationsByEndpoint = subscribers
                .GroupBy(d => d.Endpoint, d => d);

            foreach (var group in destinationsByEndpoint)
            {
                if (group.Key == null) //Routing targets that do not specify endpoint name
                {
                    //Send a message to each target as we have no idea which endpoint they represent
                    foreach (var subscriber in group)
                    {
                        yield return subscriber.TransportAddress;
                    }
                }
                else
                {
                    //TODO use a real distribution strategy
                    var subscriber = distributionPolicy.GetDistributionStrategy(group.First().Endpoint).SelectSubscriber(group.Select(s => s.TransportAddress).ToArray());
                    yield return subscriber;
//                    //Use the distribution strategy to select subset of instances of a given endpoint
//                    var destinationForEndpoint = distributionPolicy.GetDistributionStrategy(@group.Key).SelectDestination(@group.Select(t => t.Instance).ToArray());
//                    if (destinationForEndpoint != null)
//                    {
//                        yield return UnicastRoutingTarget.ToEndpointInstance(destinationForEndpoint);
//                    }
                }
            }
        }

        async Task<IEnumerable<Subscriber>> GetDestinations(ContextBag contextBag, Type[] typesToRoute)
        {
            var messageTypes = typesToRoute.Select(t => new MessageType(t));
            var subscribers = await subscriptionStorage.GetSubscriberAddressesForMessage(messageTypes, contextBag).ConfigureAwait(false);
            return subscribers;
        }

        MessageMetadataRegistry messageMetadataRegistry;
        ISubscriptionStorage subscriptionStorage;
    }
}