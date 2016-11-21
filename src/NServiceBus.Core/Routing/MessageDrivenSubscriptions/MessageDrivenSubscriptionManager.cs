namespace NServiceBus.Features
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
        public MessageDrivenSubscriptionManager(string subscriberAddress, string subscriberEndpoint, IDispatchMessages dispatcher, SubscriptionRouter subscriptionRouter)
        {
            this.subscriberAddress = subscriberAddress;
            this.subscriberEndpoint = subscriberEndpoint;
            this.dispatcher = dispatcher;
            this.subscriptionRouter = subscriptionRouter;
        }

        public async Task Subscribe(Type eventType, ContextBag context)
        {
            var publisherAddresses = subscriptionRouter.GetAddressesForEventType(eventType)
                .EnsureNonEmpty(() => $"No publisher address could be found for message type {eventType}. Ensure the configured publisher endpoint has at least one known instance.");

            var subscribeTasks = new List<Task>();
            foreach (var publisherAddress in publisherAddresses)
            {
                Logger.Debug($"Subscribing to {eventType.AssemblyQualifiedName} at publisher queue {publisherAddress}");

                var subscriptionMessage = ControlMessageFactory.Create(MessageIntentEnum.Subscribe);

                subscriptionMessage.Headers[Headers.SubscriptionMessageType] = eventType.AssemblyQualifiedName;
                subscriptionMessage.Headers[Headers.ReplyToAddress] = subscriberAddress;
                subscriptionMessage.Headers[Headers.SubscriberTransportAddress] = subscriberAddress;
                subscriptionMessage.Headers[Headers.SubscriberEndpoint] = subscriberEndpoint;
                subscriptionMessage.Headers[Headers.TimeSent] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);
                subscriptionMessage.Headers[Headers.NServiceBusVersion] = GitFlowVersion.MajorMinorPatch;

                subscribeTasks.Add(SendSubscribeMessageWithRetries(publisherAddress, subscriptionMessage, eventType.AssemblyQualifiedName, context));
            }
            await Task.WhenAll(subscribeTasks.ToArray()).ConfigureAwait(false);
        }

        public async Task Unsubscribe(Type eventType, ContextBag context)
        {
            var publisherAddresses = subscriptionRouter.GetAddressesForEventType(eventType)
                .EnsureNonEmpty(() => $"No publisher address could be found for message type {eventType}. Ensure the configured publisher endpoint has at least one known instance.");

            var unsubscribeTasks = new List<Task>();
            foreach (var publisherAddress in publisherAddresses)
            {
                Logger.Debug("Unsubscribing to " + eventType.AssemblyQualifiedName + " at publisher queue " + publisherAddress);

                var unsubscribeMessage = ControlMessageFactory.Create(MessageIntentEnum.Unsubscribe);

                unsubscribeMessage.Headers[Headers.SubscriptionMessageType] = eventType.AssemblyQualifiedName;
                unsubscribeMessage.Headers[Headers.ReplyToAddress] = subscriberAddress;
                unsubscribeMessage.Headers[Headers.SubscriberTransportAddress] = subscriberAddress;
                unsubscribeMessage.Headers[Headers.SubscriberEndpoint] = subscriberEndpoint;
                unsubscribeMessage.Headers[Headers.TimeSent] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);
                unsubscribeMessage.Headers[Headers.NServiceBusVersion] = GitFlowVersion.MajorMinorPatch;

                unsubscribeTasks.Add(SendUnsubscribeMessageWithRetries(publisherAddress, unsubscribeMessage, eventType.AssemblyQualifiedName, context));
            }
            await Task.WhenAll(unsubscribeTasks.ToArray()).ConfigureAwait(false);
        }

        async Task SendSubscribeMessageWithRetries(string destination, OutgoingMessage subscriptionMessage, string messageType, ContextBag context, int retriesCount = 0)
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

        async Task SendUnsubscribeMessageWithRetries(string destination, OutgoingMessage unsubscribeMessage, string messageType, ContextBag context, int retriesCount = 0)
        {
            var state = context.GetOrCreate<Settings>();
            try
            {
                var transportOperation = new TransportOperation(unsubscribeMessage, new UnicastAddressTag(destination));
                var transportTransaction = context.GetOrCreate<TransportTransaction>();
                await dispatcher.Dispatch(new TransportOperations(transportOperation), transportTransaction, context).ConfigureAwait(false);
            }
            catch (QueueNotFoundException ex)
            {
                if (retriesCount < state.MaxRetries)
                {
                    await Task.Delay(state.RetryDelay).ConfigureAwait(false);
                    await SendUnsubscribeMessageWithRetries(destination, unsubscribeMessage, messageType, context, ++retriesCount).ConfigureAwait(false);
                }
                else
                {
                    string message = $"Failed to unsubscribe for {messageType} at publisher queue {destination}, reason {ex.Message}";
                    Logger.Error(message, ex);
                    throw new QueueNotFoundException(destination, message, ex);
                }
            }
        }

        SubscriptionRouter subscriptionRouter;
        IDispatchMessages dispatcher;
        string subscriberAddress;
        string subscriberEndpoint;

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