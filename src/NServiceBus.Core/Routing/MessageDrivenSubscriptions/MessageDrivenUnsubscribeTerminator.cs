namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Routing.MessageDrivenSubscriptions;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Queuing;
    using NServiceBus.Unicast.Transport;

    class MessageDrivenUnsubscribeTerminator : PipelineTerminator<UnsubscribeContext>
    {
        public MessageDrivenUnsubscribeTerminator(SubscriptionRouter subscriptionRouter, string replyToAddress, IDispatchMessages dispatcher)
        {
            this.subscriptionRouter = subscriptionRouter;
            this.replyToAddress = replyToAddress;
            this.dispatcher = dispatcher;
        }

        protected override Task Terminate(UnsubscribeContext context)
        {
            var eventType = context.EventType;

            var publisherAddresses = subscriptionRouter.GetAddressesForEventType(eventType)
                .ToList();

            if (!publisherAddresses.Any())
            {
                throw new Exception($"No destination could be found for message type {eventType}. Check the <MessageEndpointMappings> section of the configuration of this endpoint for an entry either for this specific message type or for its assembly.");
            }

            var unsubscribeTasks = new List<Task>();
            foreach (var publisherAddress in publisherAddresses)
            {
                Logger.Debug("Unsubscribing to " + eventType.AssemblyQualifiedName + " at publisher queue " + publisherAddress);

                var unsubscribeMessage = ControlMessageFactory.Create(MessageIntentEnum.Unsubscribe);

                unsubscribeMessage.Headers[Headers.SubscriptionMessageType] = eventType.AssemblyQualifiedName;
                unsubscribeMessage.Headers[Headers.ReplyToAddress] = replyToAddress;

                var address = publisherAddress;

                unsubscribeTasks.Add(SendUnsubscribeMessageWithRetries(address, unsubscribeMessage, eventType.AssemblyQualifiedName, context));
            }

            return Task.WhenAll(unsubscribeTasks.ToArray());
        }

        async Task SendUnsubscribeMessageWithRetries(string destination, OutgoingMessage unsubscribeMessage, string messageType, ContextBag context, int retriesCount = 0)
        {
            try
            {
                var dispatchOptions = new DispatchOptions(new DirectToTargetDestination(destination), context);
                await dispatcher.Dispatch(new[] { new TransportOperation(unsubscribeMessage, dispatchOptions) }).ConfigureAwait(false);
            }
            catch (QueueNotFoundException ex)
            {
                if (retriesCount < 10)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
                    await SendUnsubscribeMessageWithRetries(destination, unsubscribeMessage, messageType, context, ++retriesCount).ConfigureAwait(false);
                }
                else
                {
                    string message = $"Failed to unsubsribe for {messageType} at publisher queue {destination}, reason {ex.Message}";
                    Logger.Error(message, ex);
                    throw new QueueNotFoundException(destination, message, ex);
                }
            }
        }

        SubscriptionRouter subscriptionRouter;
        string replyToAddress;
        IDispatchMessages dispatcher;

        static ILog Logger = LogManager.GetLogger<MessageDrivenUnsubscribeTerminator>();
    }
}