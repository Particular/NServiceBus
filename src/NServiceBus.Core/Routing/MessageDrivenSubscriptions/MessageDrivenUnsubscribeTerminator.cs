namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
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
        public MessageDrivenUnsubscribeTerminator(SubscriptionRouter subscriptionRouter, string replyToAddress, TransportDispatcher dispatcher)
        {
            this.subscriptionRouter = subscriptionRouter;
            this.replyToAddress = replyToAddress;
            this.dispatcher = dispatcher;
        }

        protected override Task Terminate(UnsubscribeContext context)
        {
            var eventType = context.EventType;

            var publisherAddresses = subscriptionRouter.GetAddressesForEventType(eventType)
                .EnsureNonEmpty(() => string.Format("No publisher address could be found for message type {0}. Please ensure the configured publisher endpoint has at least one known instance.", eventType));

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
            var state = context.GetOrCreate<Settings>();
            try
            {

                var dispatchOptions = new DispatchOptions(new UnicastAddressTag(destination, new Dictionary<string, string>()), DispatchConsistency.Default);
                await dispatcher.UseDefaultDispatcher(d => d.Dispatch(new[] { new TransportOperation(unsubscribeMessage, dispatchOptions) }, context)).ConfigureAwait(false);
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
                    string message = $"Failed to unsubsribe for {messageType} at publisher queue {destination}, reason {ex.Message}";
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
        TransportDispatcher dispatcher;

        static ILog Logger = LogManager.GetLogger<MessageDrivenUnsubscribeTerminator>();
    }
}