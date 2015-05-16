namespace NServiceBus.Routing.StorageDrivenPublishing
{
    using System.Collections.Generic;
    using System.Linq;
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
        public StorageDrivenDispatcher(ISubscriptionStorage subscriptionStorage, MessageMetadataRegistry messageMetadataRegistry)
        {
            this.subscriptionStorage = subscriptionStorage;
            this.messageMetadataRegistry = messageMetadataRegistry;
        }


        public override void Dispatch(IDispatchMessages dispatcher, OutgoingMessage message, RoutingStrategy routingStrategy, ConsistencyGuarantee minimumConsistencyGuarantee, IEnumerable<DeliveryConstraint> constraints, BehaviorContext currentContext)
        {
            var currentConstraints = constraints.ToList();

            var toAllSubscribersStrategy = routingStrategy as ToAllSubscribers;

            if (toAllSubscribersStrategy == null)
            {
                dispatcher.Dispatch(message, new DispatchOptions(routingStrategy,
                    minimumConsistencyGuarantee,
                    currentConstraints,
                    currentContext));

                return;
            }


            var eventType = toAllSubscribersStrategy.EventType;

            var eventTypesToPublish = messageMetadataRegistry.GetMessageMetadata(eventType)
                .MessageHierarchy
                .Distinct()
                .ToList();

            var subscribers = subscriptionStorage.GetSubscriberAddressesForMessage(eventTypesToPublish.Select(t => new MessageType(t))).ToList();


            currentContext.Set(new SubscribersForEvent(subscribers, eventType));


            if (!subscribers.Any())
            {
                return;
            }

            foreach (var subscriber in subscribers)
            {
                dispatcher.Dispatch(message, new DispatchOptions(subscriber,
                    minimumConsistencyGuarantee,
                    currentConstraints,
                    currentContext));
            }
        }

        readonly ISubscriptionStorage subscriptionStorage;
        readonly MessageMetadataRegistry messageMetadataRegistry;
    }
}