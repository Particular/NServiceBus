namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using Pipeline;
    using Routing;
    using Unicast.Messages;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    class UnicastPublishRouter : IPublishRouter
    {
        public UnicastPublishRouter(MessageMetadataRegistry messageMetadataRegistry, ISubscriptionStorage subscriptionStorage, IDistributionPolicy distributionPolicy)
        {
            this.messageMetadataRegistry = messageMetadataRegistry;
            this.subscriptionStorage = subscriptionStorage;
            this.distributionPolicy = distributionPolicy;
        }

        public async Task<RoutingStrategy[]> GetRoutingStrategies(IOutgoingPublishContext context)
        {
            var typesToRoute = messageMetadataRegistry.GetMessageMetadata(context.Message.MessageType).MessageHierarchy;

            var subscribers = await GetSubscribers(context.Extensions, typesToRoute).ConfigureAwait(false);

            var selectedDestinations = SelectDestinationsForEachEndpoint(distributionPolicy, subscribers);

            return selectedDestinations.Select(destination => new UnicastRoutingStrategy(destination)).ToArray();
        }

        static HashSet<string> SelectDestinationsForEachEndpoint(IDistributionPolicy distributionPolicy, IEnumerable<Subscriber> subscribers)
        {
            //Make sure we are sending only one to each transport destination. Might happen when there are multiple routing information sources.
            var addresses = new HashSet<string>();
            var destinationsByEndpoint = subscribers
                .GroupBy(d => d.Endpoint, d => d);

            foreach (var group in destinationsByEndpoint)
            {
                if (group.Key == null) //Routing targets that do not specify endpoint name
                {
                    //Send a message to each target as we have no idea which endpoint they represent
                    foreach (var subscriber in group)
                    {
                        addresses.Add(subscriber.TransportAddress);
                    }
                }
                else
                {
                    var subscriber = distributionPolicy.GetDistributionStrategy(group.First().Endpoint, DistributionStrategyScope.Publish).SelectReceiver(group.Select(s => s.TransportAddress).ToArray());
                    addresses.Add(subscriber);
                }
            }

            return addresses;
        }

        async Task<IEnumerable<Subscriber>> GetSubscribers(ContextBag contextBag, Type[] typesToRoute)
        {
            var messageTypes = typesToRoute.Select(t => new MessageType(t));
            var subscribers = await subscriptionStorage.GetSubscriberAddressesForMessage(messageTypes, contextBag).ConfigureAwait(false);
            return subscribers;
        }

        readonly IDistributionPolicy distributionPolicy;

        MessageMetadataRegistry messageMetadataRegistry;

        ISubscriptionStorage subscriptionStorage;
    }
}