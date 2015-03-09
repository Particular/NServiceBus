﻿namespace NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions
{
    using System;
    using System.Threading;
    using Logging;
    using Queuing;
    using Transport;
    using Transports;

    class SubscriptionManager : IManageSubscriptions
    {
        readonly string publicReturnAddress;
        readonly ISendMessages messageSender;

        public SubscriptionManager(string publicReturnAddress,ISendMessages messageSender)
        {
            this.publicReturnAddress = publicReturnAddress;
            this.messageSender = messageSender;
        }

        public void Subscribe(Type eventType, string publisherAddress)
        {
            if (publisherAddress == null)
            {
                throw new InvalidOperationException(string.Format("No destination could be found for message type {0}. Check the <MessageEndpointMappings> section of the configuration of this endpoint for an entry either for this specific message type or for its assembly.", eventType));
            }

            Logger.Info("Subscribing to " + eventType.AssemblyQualifiedName + " at publisher queue " + publisherAddress);

            var subscriptionMessage = CreateControlMessage(eventType);
            subscriptionMessage.MessageIntent = MessageIntentEnum.Subscribe;

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

            var subscriptionMessage = CreateControlMessage(eventType);
            subscriptionMessage.MessageIntent = MessageIntentEnum.Unsubscribe;

            messageSender.Send(new OutgoingMessage(subscriptionMessage.Body), new SendOptions(publisherAddress)
            {
                ReplyToAddress = publicReturnAddress,
                Headers = subscriptionMessage.Headers
            });
        }

        static TransportMessage CreateControlMessage(Type eventType)
        {
            var subscriptionMessage = ControlMessage.Create();

            subscriptionMessage.Headers[Headers.SubscriptionMessageType] = eventType.AssemblyQualifiedName;
            return subscriptionMessage;
        }

        void SendSubscribeMessageWithRetries(string destination, TransportMessage subscriptionMessage, string messageType, int retriesCount = 0)
        {
            try
            {
                messageSender.Send(new OutgoingMessage(subscriptionMessage.Body), new SendOptions(destination)
                {
                    Headers = subscriptionMessage.Headers,
                    ReplyToAddress = publicReturnAddress 
                });
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