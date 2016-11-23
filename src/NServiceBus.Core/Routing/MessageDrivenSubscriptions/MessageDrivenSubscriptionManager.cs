namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;
    using Logging;
    using Routing;
    using Transport;
    using Unicast.Queuing;
    using Unicast.Transport;

    class MessageDrivenSubscriptionManager : IManageSubscriptions
    {
        public MessageDrivenSubscriptionManager(SubscriptionRouter subscriptionRouter, string subscriberAddress, string subscriberEndpoint, IDispatchMessages dispatcher)
        {
            this.subscriptionRouter = subscriptionRouter;
            this.subscriberAddress = subscriberAddress;
            this.subscriberEndpoint = subscriberEndpoint;
            this.dispatcher = dispatcher;
        }

        public Task Subscribe(Type eventType, ContextBag context)
        {
            return SendRequest(eventType, MessageIntentEnum.Subscribe, context);
        }

        public Task Unsubscribe(Type eventType, ContextBag context)
        {
            return SendRequest(eventType, MessageIntentEnum.Unsubscribe, context);
        }

        async Task SendRequest(Type eventType, MessageIntentEnum intent, ContextBag context)
        {
            var operation = intent == MessageIntentEnum.Subscribe ? "Subscribing" : "Unsubscribing";
            var publisherAddresses = subscriptionRouter.GetAddressesForEventType(eventType)
                .EnsureNonEmpty(() => $"No publisher address could be found for message type {eventType}. Ensure the configured publisher endpoint has at least one known instance.");

            var subscribeTasks = new List<Task>();
            foreach (var publisherAddress in publisherAddresses)
            {
                Logger.Debug($"{operation} to {eventType.AssemblyQualifiedName} at publisher queue {publisherAddress}");

                var subscriptionMessage = ControlMessageFactory.Create(intent);

                subscriptionMessage.Headers[Headers.SubscriptionMessageType] = eventType.AssemblyQualifiedName;
                subscriptionMessage.Headers[Headers.ReplyToAddress] = subscriberAddress;
                subscriptionMessage.Headers[Headers.SubscriberTransportAddress] = subscriberAddress;
                subscriptionMessage.Headers[Headers.SubscriberEndpoint] = subscriberEndpoint;
                subscriptionMessage.Headers[Headers.TimeSent] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);
                subscriptionMessage.Headers[Headers.NServiceBusVersion] = GitFlowVersion.MajorMinorPatch;

                subscribeTasks.Add(SendMessageWithRetries(publisherAddress, subscriptionMessage, eventType.AssemblyQualifiedName, operation, context));
            }
            await Task.WhenAll(subscribeTasks.ToArray()).ConfigureAwait(false);
        }

        async Task SendMessageWithRetries(string destination, OutgoingMessage subscriptionMessage, string messageType, string operation, ContextBag context, int retriesCount = 0)
        {
            var state = context.GetOrCreate<Settings>();
            try
            {
                var transportOperation = new TransportOperation(subscriptionMessage, new UnicastAddressTag(destination));
                var transportTransaction = context.GetOrCreate<TransportTransaction>();
                await dispatcher.Dispatch(new TransportOperations(transportOperation), transportTransaction, context).ConfigureAwait(false);
            }
            catch (QueueNotFoundException ex)
            {
                if (retriesCount < state.MaxRetries)
                {
                    await Task.Delay(state.RetryDelay).ConfigureAwait(false);
                    await SendMessageWithRetries(destination, subscriptionMessage, messageType, operation, context, ++retriesCount).ConfigureAwait(false);
                }
                else
                {
                    string message = $"{operation} to {messageType} failed at publisher queue {destination}, reason {ex.Message}";
                    Logger.Error(message, ex);
                    throw new QueueNotFoundException(destination, message, ex);
                }
            }
        }

        IDispatchMessages dispatcher;
        string subscriberAddress;
        string subscriberEndpoint;

        SubscriptionRouter subscriptionRouter;

        static ILog Logger = LogManager.GetLogger<MessageDrivenSubscriptionManager>();

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

    }
}
