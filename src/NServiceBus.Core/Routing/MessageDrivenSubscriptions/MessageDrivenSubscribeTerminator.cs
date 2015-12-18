namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Queuing;
    using NServiceBus.Unicast.Transport;

    class MessageDrivenSubscribeTerminator : PipelineTerminator<ISubscribeContext>
    {
        public MessageDrivenSubscribeTerminator(SubscriptionRouter subscriptionRouter, string subscriberAddress, EndpointName subscriberEndpoint, IDispatchMessages dispatcher, bool legacyMode)
        {
            this.subscriptionRouter = subscriptionRouter;
            this.subscriberAddress = subscriberAddress;
            this.subscriberEndpoint = subscriberEndpoint;
            this.dispatcher = dispatcher;
            this.legacyMode = legacyMode;
        }

        protected override async Task Terminate(ISubscribeContext context)
        {
            var eventType = context.EventType;

            var publisherAddresses = (await subscriptionRouter.GetAddressesForEventType(eventType).ConfigureAwait(false))
                .EnsureNonEmpty(() => $"No publisher address could be found for message type {eventType}. Please ensure the configured publisher endpoint has at least one known instance.");

            var subscribeTasks = new List<Task>();
            foreach (var publisherAddress in publisherAddresses)
            {
                Logger.Debug($"Subscribing to {eventType.AssemblyQualifiedName} at publisher queue {publisherAddress}");

                var subscriptionMessage = ControlMessageFactory.Create(MessageIntentEnum.Subscribe);

                subscriptionMessage.Headers[Headers.SubscriptionMessageType] = eventType.AssemblyQualifiedName;

                if (legacyMode)
                {
                    subscriptionMessage.Headers[Headers.ReplyToAddress] = subscriberAddress;
                }
                else
                {
                    subscriptionMessage.Headers[Headers.SubscriberTransportAddress] = subscriberAddress;
                    subscriptionMessage.Headers[Headers.SubscriberEndpoint] = subscriberEndpoint.ToString();
                }
                var address = publisherAddress;

                subscribeTasks.Add(SendSubscribeMessageWithRetries(address, subscriptionMessage, eventType.AssemblyQualifiedName, context.Extensions));
            }
            await Task.WhenAll(subscribeTasks.ToArray()).ConfigureAwait(false);
        }

        async Task SendSubscribeMessageWithRetries(string destination, OutgoingMessage subscriptionMessage, string messageType, ContextBag context, int retriesCount = 0)
        {
            var state = context.GetOrCreate<Settings>();
            try
            {
                var transportOperation = new TransportOperation(subscriptionMessage, new UnicastAddressTag(destination), DispatchConsistency.Default);
                await dispatcher.Dispatch(new TransportOperations(transportOperation), context).ConfigureAwait(false);
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
        string subscriberAddress;
        EndpointName subscriberEndpoint;
        IDispatchMessages dispatcher;
        bool legacyMode;

        static ILog Logger = LogManager.GetLogger<MessageDrivenUnsubscribeTerminator>();
    }
}