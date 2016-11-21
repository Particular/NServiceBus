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
    using Unicast.Queuing;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    class UnicastPublishRouterConnector : StageConnector<IOutgoingPublishContext, IOutgoingDistributionContext>
    {
        MessageMetadataRegistry messageMetadataRegistry;
        ISubscriptionStorage subscriptionStorage;

        public UnicastPublishRouterConnector(MessageMetadataRegistry messageMetadataRegistry, ISubscriptionStorage subscriptionStorage)
        {
            this.messageMetadataRegistry = messageMetadataRegistry;
            this.subscriptionStorage = subscriptionStorage;
        }

        public override async Task Invoke(IOutgoingPublishContext context, Func<IOutgoingDistributionContext, Task> stage)
        {
            var eventType = context.Message.MessageType;
            var typesToRoute = messageMetadataRegistry.GetMessageMetadata(eventType).MessageHierarchy;
            var routes = await GetSubscribers(typesToRoute, context.Extensions).ConfigureAwait(false);
            if (routes.Count == 0)
            {
                //No subscribers for this message.
                return;
            }

            context.Headers[Headers.MessageIntent] = MessageIntentEnum.Publish.ToString();

            try
            {
                await stage(this.CreateDistributionContext(context.Message, routes, context)).ConfigureAwait(false);
            }
            catch (QueueNotFoundException ex)
            {
                throw new Exception($"The destination queue '{ex.Queue}' could not be found. The destination may be misconfigured for this kind of message ({eventType}) in the MessageEndpointMappings of the UnicastBusConfig section in the configuration file. It may also be the case that the given queue hasn\'t been created yet, or has been deleted.", ex);
            }
        }

        async Task<IReadOnlyCollection<UnicastRoute>> GetSubscribers(Type[] typesToRoute, ContextBag contextBag)
        {
            var messageTypes = typesToRoute.Select(t => new MessageType(t));
            var subscribers = await subscriptionStorage.GetSubscriberAddressesForMessage(messageTypes, contextBag).ConfigureAwait(false);

            return subscribers.Select(ToUnicastRoute).ToArray();
        }

        static UnicastRoute ToUnicastRoute(Subscriber s)
        {
            return s.Endpoint != null 
                ? UnicastRoute.CreateFromPhysicalAddress(s.TransportAddress, s.Endpoint) 
                : UnicastRoute.CreateFromPhysicalAddress(s.TransportAddress);
        }
    }
}