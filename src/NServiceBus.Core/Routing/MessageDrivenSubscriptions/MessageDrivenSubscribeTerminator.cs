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

    class MessageDrivenSubscribeTerminator : PipelineTerminator<SubscribeContext>
    {
        public MessageDrivenSubscribeTerminator(SubscriptionRouter subscriptionRouter, string replyToAddress, IDispatchMessages dispatcher)
        {
            this.subscriptionRouter = subscriptionRouter;
            this.replyToAddress = replyToAddress;
            this.dispatcher = dispatcher;
        }

        protected override Task Terminate(SubscribeContext context)
        {
            var eventType = context.EventType;

            var publisherAddresses = subscriptionRouter.GetAddressesForEventType(eventType)
                .ToList();

            if (!publisherAddresses.Any())
            {
                throw new Exception($"No destination could be found for message type {eventType}. Check the <MessageEndpointMappings> section of the configuration of this endpoint for an entry either for this specific message type or for its assembly.");
            }

            var subscribeTasks = new List<Task>();
            foreach (var publisherAddress in publisherAddresses)
            {
                Logger.Debug("Subscribing to " + eventType.AssemblyQualifiedName + " at publisher queue " + publisherAddress);

                var subscriptionMessage = ControlMessageFactory.Create(MessageIntentEnum.Subscribe);

                subscriptionMessage.Headers[Headers.SubscriptionMessageType] = eventType.AssemblyQualifiedName;
                subscriptionMessage.Headers[Headers.ReplyToAddress] = replyToAddress;

                var address = publisherAddress;

                subscribeTasks.Add(SendSubscribeMessageWithRetries(address, subscriptionMessage, eventType.AssemblyQualifiedName, context));
            }

            return Task.WhenAll(subscribeTasks.ToArray());
        }

        async Task SendSubscribeMessageWithRetries(string destination, OutgoingMessage subscriptionMessage, string messageType, ContextBag context, int retriesCount = 0)
        {
            var state = context.GetOrCreate<Settings>();
            try
            {
                var dispatchOptions = new DispatchOptions(new DirectToTargetDestination(destination));
                await dispatcher.Dispatch(new[] { new TransportOperation(subscriptionMessage, dispatchOptions) }, context).ConfigureAwait(false);
            }
            catch (QueueNotFoundException ex)
            {
                if (retriesCount < state.MaxRetries)
                {
                    await Task.Delay(state.RetryDelay).ConfigureAwait(false);
                    await SendSubscribeMessageWithRetries(destination, subscriptionMessage, messageType, context, ++retriesCount).ConfigureAwait(false);
                }
                else
                {
                    string message = $"Failed to subscribe to {messageType} at publisher queue {destination}, reason {ex.Message}";
                    Logger.Error(message, ex);
                    throw new QueueNotFoundException(destination, message, ex);
                }
            }
        }

        public class Settings
        {
            public Settings()
            {
                MaxRetries = 10;
                RetryDelay = TimeSpan.FromSeconds(2);
            }

            public TimeSpan RetryDelay { get; set; }
            public int MaxRetries { get; set; }
        }

        SubscriptionRouter subscriptionRouter;
        string replyToAddress;
        IDispatchMessages dispatcher;

        static ILog Logger = LogManager.GetLogger<MessageDrivenUnsubscribeTerminator>();
    }
}