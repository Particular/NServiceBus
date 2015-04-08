namespace NServiceBus.Unicast.Publishing
{
    using System.Linq;
    using Messages;
    using Pipeline;
    using Subscriptions;
    using Subscriptions.MessageDrivenSubscriptions;
    using Transports;


    class StorageDrivenPublisher:IPublishMessages
    {
        readonly ISubscriptionStorage subscriptionStorage;
        readonly ISendMessages messageSender;
        readonly MessageMetadataRegistry messageMetadataRegistry;
        readonly BehaviorContext context;

        public StorageDrivenPublisher(ISubscriptionStorage subscriptionStorage, ISendMessages messageSender, MessageMetadataRegistry messageMetadataRegistry, BehaviorContext context)
        {
            this.subscriptionStorage = subscriptionStorage;
            this.messageSender = messageSender;
            this.messageMetadataRegistry = messageMetadataRegistry;
            this.context = context;
        }


        public void Publish(OutgoingMessage message, PublishMessageOptions publishOptions)
        {
            var eventTypesToPublish = messageMetadataRegistry.GetMessageMetadata(publishOptions.EventType)
                .MessageHierarchy
                .Distinct()
                .ToList();

            var subscribers = subscriptionStorage.GetSubscriberAddressesForMessage(eventTypesToPublish.Select(t => new MessageType(t))).ToList();

            if (!subscribers.Any())
            {
                context.Set("NoSubscribersFoundForMessage",true);
                return;
            }

            context.Set("SubscribersForEvent", subscribers);

            foreach (var subscriber in subscribers)
            {
                messageSender.Send(message, new TransportSendOptions(subscriber));
            }
        }
    }
}