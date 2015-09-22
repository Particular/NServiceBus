namespace NServiceBus.Routing.StorageDrivenPublishing
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
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

        public async override Task Dispatch(IDispatchMessages dispatcher, OutgoingMessage message, RoutingStrategy routingStrategy, IEnumerable<DeliveryConstraint> constraints, BehaviorContext currentContext, DispatchConsistency dispatchConsistency)
        {
            var currentConstraints = constraints.ToList();

            var toAllSubscribersStrategy = routingStrategy as ToAllSubscribers;

            if (toAllSubscribersStrategy == null)
            {
                var dispatchOptions = new DispatchOptions(routingStrategy,
                    currentContext,
                    currentConstraints,
                    dispatchConsistency);
                await dispatcher.Dispatch(new [] {new TransportOperation(message, dispatchOptions)}).ConfigureAwait(false);
                return;
            }


            var eventType = toAllSubscribersStrategy.EventType;

            var eventTypesToPublish = messageMetadataRegistry.GetMessageMetadata(eventType)
                .MessageHierarchy
                .Distinct()
                .ToList();

            var subscribers = (await querySubscriptions.GetSubscriberAddressesForMessage(eventTypesToPublish.Select(t => new MessageType(t))).ConfigureAwait(false))
                .ToList();

            currentContext.Set(new SubscribersForEvent(subscribers, eventType));

            if (!subscribers.Any())
            {
                return;
            }

            foreach (var subscriber in subscribers)
            {
                var dispatchOptions = new DispatchOptions(new DirectToTargetDestination(subscriber),
                    currentContext,
                    currentConstraints,
                    dispatchConsistency);
                await dispatcher.Dispatch(new [] { new TransportOperation(message, dispatchOptions)}).ConfigureAwait(false);
            }
        }

        IQuerySubscriptions querySubscriptions;
        MessageMetadataRegistry messageMetadataRegistry;
    }
}