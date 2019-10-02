namespace NServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using Logging;
    using Pipeline;
    using Routing;
    using Unicast.Messages;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    class UnicastPublishRouter : IUnicastPublishRouter
    {
        public UnicastPublishRouter(MessageMetadataRegistry messageMetadataRegistry, Func<EndpointInstance, string> transportAddressTranslation, ISubscriptionStorage subscriptionStorage)
        {
            this.messageMetadataRegistry = messageMetadataRegistry;
            this.transportAddressTranslation = transportAddressTranslation;
            this.subscriptionStorage = subscriptionStorage;
        }

        public async Task<IEnumerable<UnicastRoutingStrategy>> Route(Type messageType, IDistributionPolicy distributionPolicy, IOutgoingPublishContext publishContext)
        {
            var typesToRoute = messageMetadataRegistry.GetMessageMetadata(messageType).MessageHierarchy;

            var subscribers = await GetSubscribers(publishContext, typesToRoute).ConfigureAwait(false);

            var destinations = SelectDestinationsForEachEndpoint(publishContext, distributionPolicy, subscribers);

            WarnIfNoSubscribersFound(messageType, destinations.Count);

            return destinations;
        }

        void WarnIfNoSubscribersFound(Type messageType, int subscribersFound)
        {
            if (subscribersFound == 0)
            {
                eventsWithoutSubscribers.GetOrAdd(messageType.FullName, @event =>
                {
                    logger.WarnFormat("No subscribers found for the event of type {0}.", @event);

                    return true;
                });
            }
        }

        Dictionary<string, UnicastRoutingStrategy>.ValueCollection SelectDestinationsForEachEndpoint(IOutgoingPublishContext publishContext, IDistributionPolicy distributionPolicy, IEnumerable<Subscriber> subscribers)
        {
            //Make sure we are sending only one to each transport destination. Might happen when there are multiple routing information sources.
            var addresses = new Dictionary<string, UnicastRoutingStrategy>();
            Dictionary<string, List<string>> groups = null;
            foreach (var subscriber in subscribers)
            {
                if(subscriber.Endpoint == null)
                {
                    if (!addresses.ContainsKey(subscriber.TransportAddress))
                    {
                        addresses.Add(subscriber.TransportAddress, new UnicastRoutingStrategy(subscriber.TransportAddress));
                    }

                    continue;
                }

                groups = groups ?? new Dictionary<string, List<string>>();

                if (groups.TryGetValue(subscriber.Endpoint, out var transportAddresses))
                {
                    transportAddresses.Add(subscriber.TransportAddress);
                }
                else
                {
                    groups[subscriber.Endpoint] = new List<string> { subscriber.TransportAddress };
                }
            }

            if (groups != null)
            {
                foreach (var group in groups)
                {
                    var instances = group.Value.ToArray(); // could we avoid this?
                    var distributionContext = new DistributionContext(instances, publishContext.Message, publishContext.MessageId, publishContext.Headers, transportAddressTranslation, publishContext.Extensions);
                    var subscriber = distributionPolicy.GetDistributionStrategy(group.Key, DistributionStrategyScope.Publish).SelectDestination(distributionContext);

                    if (!addresses.ContainsKey(subscriber))
                    {
                        addresses.Add(subscriber, new UnicastRoutingStrategy(subscriber));
                    }
                }
            }

            return addresses.Values;
        }

        Task<IEnumerable<Subscriber>> GetSubscribers(IExtendable publishContext, Type[] typesToRoute)
        {
            var messageTypes = typesToRoute.Select(t => new MessageType(t));
            return subscriptionStorage.GetSubscriberAddressesForMessage(messageTypes, publishContext.Extensions);
        }

        MessageMetadataRegistry messageMetadataRegistry;
        Func<EndpointInstance, string> transportAddressTranslation;
        ISubscriptionStorage subscriptionStorage;
        ConcurrentDictionary<string, bool> eventsWithoutSubscribers = new ConcurrentDictionary<string, bool>();
        static ILog logger = LogManager.GetLogger<UnicastPublishRouter>();
    }
}