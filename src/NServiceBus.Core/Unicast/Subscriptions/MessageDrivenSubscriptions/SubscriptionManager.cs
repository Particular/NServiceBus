namespace NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions
{
    using System;
    using System.Threading;
    using NServiceBus.Logging;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Queuing;
    using NServiceBus.Unicast.Transport;

    class SubscriptionManager : IManageSubscriptions
    {
        readonly string replyToAddress;
        readonly ISendMessages messageSender;

        public SubscriptionManager(string replyToAddress,ISendMessages messageSender)
        {
            this.replyToAddress = replyToAddress;
            this.messageSender = messageSender;
        }

        public void Subscribe(Type eventType, string publisherAddress)
        {
            if (publisherAddress == null)
            {
                throw new InvalidOperationException(string.Format("No destination could be found for message type {0}. Check the <MessageEndpointMappings> section of the configuration of this endpoint for an entry either for this specific message type or for its assembly.", eventType));
            }

            Logger.Info("Subscribing to " + eventType.AssemblyQualifiedName + " at publisher queue " + publisherAddress);

            var subscriptionMessage = CreateControlMessage(eventType, MessageIntentEnum.Subscribe);

           
            ThreadPool.QueueUserWorkItem(state =>
                SendSubscribeMessageWithRetries(publisherAddress, subscriptionMessage, eventType.AssemblyQualifiedName));
        }

        public void Unsubscribe(Type eventType, string publisherAddress)
        {
            if (publisherAddress == null)
            {
                throw new InvalidOperationException(string.Format("No destination could be found for message type {0}. Check the <MessageEndpointMapping> section of the configuration of this endpoint for an entry either for this specific message type or for its assembly.", eventType));
            }

            Logger.Info("Unsubscribing from " + eventType.AssemblyQualifiedName + " at publisher queue " + publisherAddress);

            var subscriptionMessage = CreateControlMessage(eventType,MessageIntentEnum.Unsubscribe);
   
            messageSender.Send(subscriptionMessage, new TransportSendOptions(publisherAddress));
        }

        OutgoingMessage CreateControlMessage(Type eventType,MessageIntentEnum intent)
        {
            var subscriptionMessage = ControlMessageFactory.Create(intent);

            subscriptionMessage.Headers[Headers.SubscriptionMessageType] = eventType.AssemblyQualifiedName;
            subscriptionMessage.Headers[Headers.ReplyToAddress] = replyToAddress;

            return subscriptionMessage;
        }

        void SendSubscribeMessageWithRetries(string destination, OutgoingMessage subscriptionMessage, string messageType, int retriesCount = 0)
        {
            try
            {
                messageSender.Send(subscriptionMessage, new TransportSendOptions(destination));
            }
            catch (QueueNotFoundException ex)
            {
                if (retriesCount < 10)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(2));
                    SendSubscribeMessageWithRetries(destination, subscriptionMessage, messageType, ++retriesCount);
                }
                else
                {
                    Logger.ErrorFormat("Failed to subscribe to {0} at publisher queue {1}", ex, messageType, destination);
                }
            }
        }

        static ILog Logger = LogManager.GetLogger<SubscriptionManager>();
    }
}