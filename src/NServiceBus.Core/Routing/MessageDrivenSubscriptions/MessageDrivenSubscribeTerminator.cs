namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Threading;
    using NServiceBus.Extensibility;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Routing.MessageDrivenSubscriptions;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Queuing;
    using NServiceBus.Unicast.Transport;

    class MessageDrivenSubscribeTerminator : PipelineTerminator<SubscribeContext>
    {
        public MessageDrivenSubscribeTerminator(SubscriptionRouter subscriptionRouter, string replyToAddress, IDispatchMessages dispatcher)
        {
            this.subscriptionRouter = subscriptionRouter;
            this.replyToAddress = replyToAddress;
            this.dispatcher = dispatcher;
        }

        public override void Terminate(SubscribeContext context)
        {
            var eventType = context.EventType;

            var publisherAddresses = subscriptionRouter.GetAddressesForEventType(eventType)
                .ToList();

            if (!publisherAddresses.Any())
            {
                throw new Exception(string.Format("No destination could be found for message type {0}. Check the <MessageEndpointMappings> section of the configuration of this endpoint for an entry either for this specific message type or for its assembly.", eventType));
            }

            foreach (var publisherAddress in publisherAddresses)
            {
                Logger.Debug("Subscribing to " + eventType.AssemblyQualifiedName + " at publisher queue " + publisherAddress);

                var subscriptionMessage = ControlMessageFactory.Create(MessageIntentEnum.Subscribe);

                subscriptionMessage.Headers[Headers.SubscriptionMessageType] = eventType.AssemblyQualifiedName;
                subscriptionMessage.Headers[Headers.ReplyToAddress] = replyToAddress;

                var address = publisherAddress;

                ThreadPool.QueueUserWorkItem(state =>
                    SendSubscribeMessageWithRetries(address, subscriptionMessage, eventType.AssemblyQualifiedName, context));
            }
        }

        void SendSubscribeMessageWithRetries(string destination, OutgoingMessage subscriptionMessage, string messageType, ContextBag context, int retriesCount = 0)
        {
            try
            {
                var dispatchOptions = new DispatchOptions(new DirectToTargetDestination(destination), context);
                dispatcher.Dispatch(new [] { new TransportOperation(subscriptionMessage, dispatchOptions)}).GetAwaiter().GetResult();
            }
            catch (QueueNotFoundException ex)
            {
                if (retriesCount < 10)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(2));
                    SendSubscribeMessageWithRetries(destination, subscriptionMessage, messageType, context, ++retriesCount);
                }
                else
                {
                    Logger.ErrorFormat("Failed to subscribe to {0} at publisher queue {1}, reason {2}", messageType, destination, ex.Message);
                }
            }
        }

        SubscriptionRouter subscriptionRouter;
        string replyToAddress;
        IDispatchMessages dispatcher;

        static ILog Logger = LogManager.GetLogger<MessageDrivenUnsubscribeTerminator>();
    }
}