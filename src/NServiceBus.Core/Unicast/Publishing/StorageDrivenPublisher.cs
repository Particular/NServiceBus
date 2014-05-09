namespace NServiceBus.Unicast.Publishing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using IdGeneration;
    using Pipeline;
    using Subscriptions;
    using Subscriptions.MessageDrivenSubscriptions;
    using Transports;

    class StorageDrivenPublisher:IPublishMessages
    {
        public ISubscriptionStorage SubscriptionStorage { get; set; }

        public ISendMessages MessageSender{ get; set; }

        public PipelineExecutor PipelineExecutor { get; set; }
      
        public bool Publish(TransportMessage message, IEnumerable<Type> eventTypes)
        {
            if (SubscriptionStorage == null)
            {
                throw new InvalidOperationException("Cannot publish on this endpoint - no subscription storage has been configured. Please see: http://docs.particular.net/nservicebus/publish-subscribe-configuration'");
            }
                
            var subscribers = SubscriptionStorage.GetSubscriberAddressesForMessage(eventTypes.Select(t => new MessageType(t))).ToList();

            if (!subscribers.Any())
            {
                PipelineExecutor.CurrentContext.Set("NoSubscribersFoundForMessage",true);
                return false;
            }
                

            foreach (var subscriber in subscribers)
            {
                //this is unicast so we give the message a unique ID
                message.ChangeMessageId(CombGuid.Generate().ToString());

                MessageSender.Send(message,subscriber);
            }

            return true;
        }
    }
}