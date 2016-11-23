namespace NServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using Pipeline;
    using Routing;
    using Unicast.Messages;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    class RefreshSubscribersBehavior : Behavior<IOutgoingPublishContext>
    {
        public RefreshSubscribersBehavior(ISubscriptionStorage subscriptionStorage, UnicastSubscriberTable subscriberTable, MessageMetadataRegistry messageMetadataRegistry, TimeSpan cachePeriod)
        {
            this.subscriptionStorage = subscriptionStorage;
            this.subscriberTable = subscriberTable;
            this.messageMetadataRegistry = messageMetadataRegistry;
            this.cachePeriod = cachePeriod;
        }

        public override async Task Invoke(IOutgoingPublishContext context, Func<Task> next)
        {
            var now = DateTime.UtcNow;
            var previous = DateTime.MinValue;
            refreshTimes.AddOrUpdate(context.Message.MessageType, t => now, (t, time) =>
            {
                previous = time;
                return now;
            });
            if (now - previous >= cachePeriod)
            {
                await RefreshSubscriberTable(context.Message.MessageType, context).ConfigureAwait(false);
            }
            await next().ConfigureAwait(false);
        }

        async Task RefreshSubscriberTable(Type messageType, IExtendable context)
        {
            var typesToRoute = messageMetadataRegistry.GetMessageMetadata(messageType).MessageHierarchy;
            var currentSubscriptions = await subscriptionStorage.GetSubscriberAddressesForMessage(typesToRoute.Select(t => new MessageType(t)).ToArray(), context.Extensions).ConfigureAwait(false);
            var entries = currentSubscriptions.Select(s => new RouteTableEntry(messageType, UnicastRoute.CreateFromPhysicalAddress(s.TransportAddress, s.Endpoint)));
            subscriberTable.AddOrReplaceRoutes(messageType.FullName, entries.ToList()); //We ignore version property of MessageType
        }

        ISubscriptionStorage subscriptionStorage;
        UnicastSubscriberTable subscriberTable;
        MessageMetadataRegistry messageMetadataRegistry;
        TimeSpan cachePeriod;
        ConcurrentDictionary<Type, DateTime> refreshTimes = new ConcurrentDictionary<Type, DateTime>();
    }
}