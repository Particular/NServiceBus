namespace NServiceBus.Routing.StorageDrivenPublishing
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.ConsistencyGuarantees;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Pipeline;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Messages;
    using NServiceBus.Unicast.Subscriptions;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

    class StorageDrivenDispatcher : DispatchStrategy
    {
        public StorageDrivenDispatcher(IQuerySubscriptions querySubscriptions, MessageMetadataRegistry messageMetadataRegistry)
        {
            this.querySubscriptions = querySubscriptions;
            this.messageMetadataRegistry = messageMetadataRegistry;
        }

        public override async Task Dispatch(IDispatchMessages dispatcher, OutgoingMessage message, RoutingStrategy routingStrategy, ConsistencyGuarantee minimumConsistencyGuarantee, IEnumerable<DeliveryConstraint> constraints, BehaviorContext currentContext)
        {
            var currentConstraints = constraints.ToList();

            var toAllSubscribersStrategy = routingStrategy as ToAllSubscribers;

            if (toAllSubscribersStrategy == null)
            {
                await dispatcher.Dispatch(message, new DispatchOptions(routingStrategy,
                    minimumConsistencyGuarantee,
                    currentConstraints,
                    currentContext)).ConfigureAwait(false);

                return;
            }


            var eventType = toAllSubscribersStrategy.EventType;

            var eventTypesToPublish = messageMetadataRegistry.GetMessageMetadata(eventType)
                .MessageHierarchy
                .Distinct()
                .ToList();

            var subscribers = querySubscriptions.GetSubscriberAddressesForMessage(eventTypesToPublish.Select(t => new MessageType(t))).GetAwaiter().GetResult()
                .ToList();

            currentContext.Set(new SubscribersForEvent(subscribers, eventType));

            if (!subscribers.Any())
            {
                return;
            }

            foreach (var subscriber in subscribers)
            {
                await dispatcher.Dispatch(message, new DispatchOptions(subscriber,
                    minimumConsistencyGuarantee,
                    currentConstraints,
                    currentContext)).ConfigureAwait(false);
            }
        }

        IQuerySubscriptions querySubscriptions;
        MessageMetadataRegistry messageMetadataRegistry;
    }
}