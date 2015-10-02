namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Pipeline;
    using Routing;
    using Transports;
    using Unicast.Messages;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    class StorageDrivenPublishBehavior : Behavior<DispatchContext>
    {
        public StorageDrivenPublishBehavior(ISubscriptionStorage querySubscriptions, MessageMetadataRegistry messageMetadataRegistry)
        {
            this.querySubscriptions = querySubscriptions;
            this.messageMetadataRegistry = messageMetadataRegistry;
        }

        public async override Task Invoke(DispatchContext context, Func<Task> next)
        {
            var publishOperations = context.Operations.Where(op => op.DispatchOptions.RoutingStrategy is ToAllSubscribers)
                .ToList();

            if (publishOperations.Any())
            {
                var newOps = context.Operations.Where(op => op.DispatchOptions.RoutingStrategy is DirectToTargetDestination)
                    .ToList();

                foreach (var publishOperation in publishOperations)
                {
                    var toAllSubscribersStrategy = (ToAllSubscribers)publishOperation.DispatchOptions.RoutingStrategy;
                    var eventType = toAllSubscribersStrategy.EventType;

                    var eventTypesToPublish = messageMetadataRegistry.GetMessageMetadata(eventType)
                        .MessageHierarchy
                        .Distinct()
                        .ToList();

                    var subscribers = (await querySubscriptions.GetSubscriberAddressesForMessage(eventTypesToPublish.Select(t => new MessageType(t)), new SubscriptionStorageOptions(context))
                        .ConfigureAwait(false)).ToList();

                    newOps.AddRange(subscribers.Select(subscriber => new TransportOperation(publishOperation.Message, new DispatchOptions(new DirectToTargetDestination(subscriber), publishOperation.DispatchOptions.RequiredDispatchConsistency, publishOperation.DispatchOptions.DeliveryConstraints))));
                }

                context.Replace(newOps);
            }

            await next().ConfigureAwait(false);
        }

        ISubscriptionStorage querySubscriptions;
        MessageMetadataRegistry messageMetadataRegistry;
    }
}