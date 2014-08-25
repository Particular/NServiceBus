namespace NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions
{
    using System;
    using System.Threading;
    using Logging;
    using Queuing;
    using Transport;
    using Transports;

    class SubscriptionManager : IManageSubscriptions
    {
        public ISendMessages MessageSender { get; set; }
        public ISubscriptionStorage SubscriptionStorage { get; set; }

        public Configure Configure { get; set; }

        public void Subscribe(Type eventType, string publisherAddress)
        {
            Logger.Info("Subscribing to " + eventType.AssemblyQualifiedName + " at publisher queue " + publisherAddress);

            var subscriptionMessage = CreateControlMessage(eventType);
            subscriptionMessage.MessageIntent = MessageIntentEnum.Subscribe;

            ThreadPool.QueueUserWorkItem(state =>
                SendSubscribeMessageWithRetries(publisherAddress, subscriptionMessage, eventType.AssemblyQualifiedName));
        }

        public void Unsubscribe(Type eventType, string publisherAddress)
        {
            Logger.Info("Unsubscribing from " + eventType.AssemblyQualifiedName + " at publisher queue " + publisherAddress);

            var subscriptionMessage = CreateControlMessage(eventType);
            subscriptionMessage.MessageIntent = MessageIntentEnum.Unsubscribe;

            MessageSender.Send(subscriptionMessage, new SendOptions(publisherAddress)
            {
                ReplyToAddress = Configure.PublicReturnAddress
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
                MessageSender.Send(subscriptionMessage, new SendOptions(destination)
                {
                    ReplyToAddress = Configure.PublicReturnAddress
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