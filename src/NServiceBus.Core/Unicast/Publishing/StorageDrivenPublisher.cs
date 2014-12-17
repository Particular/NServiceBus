namespace NServiceBus.Unicast.Publishing
{
    using System;
    using System.Linq;
    using Messages;
    using NServiceBus.Routing;
    using Pipeline;
    using Subscriptions;
    using Subscriptions.MessageDrivenSubscriptions;
    using Transports;

    class StorageDrivenPublisher:IPublishMessages
    {
        public ISubscriptionStorage SubscriptionStorage { get; set; }

        public ISendMessages MessageSender{ get; set; }

        public PipelineExecutor PipelineExecutor { get; set; }
      
        public MessageMetadataRegistry MessageMetadataRegistry { get; set; }

        public DynamicRoutingProvider RoutingProvider { get; set; }


        public void Publish(TransportMessage message, PublishOptions publishOptions)
        {
            if (SubscriptionStorage == null)
            {
                throw new InvalidOperationException("Cannot publish on this endpoint - no subscription storage has been configured.");
            }
                
            var eventTypesToPublish = MessageMetadataRegistry.GetMessageMetadata(publishOptions.EventType.FullName)
                .MessageHierarchy
                .Distinct()
                .ToList();

            var subscribers = SubscriptionStorage.GetSubscriberAddressesForMessage(eventTypesToPublish.Select(t => new MessageType(t))).ToList();

            if (!subscribers.Any())
            {
                PipelineExecutor.CurrentContext.Set("NoSubscribersFoundForMessage",true);
                return;
            }

            PipelineExecutor.CurrentContext.Set("SubscribersForEvent", subscribers);

            foreach (var subscriber in subscribers)
            {
                //this is unicast so we give the message a unique ID
                message.ChangeMessageId(CombGuid.Generate().ToString());


                var address = subscriber;

                address = RoutingProvider.GetRouteAddress(address);

                MessageSender.Send(message, new SendOptions(address)
                {
                    ReplyToAddress = publishOptions.ReplyToAddress,
                    EnforceMessagingBestPractices = publishOptions.EnforceMessagingBestPractices,
                    EnlistInReceiveTransaction = publishOptions.EnlistInReceiveTransaction,
                });
            }
        }
    }
}