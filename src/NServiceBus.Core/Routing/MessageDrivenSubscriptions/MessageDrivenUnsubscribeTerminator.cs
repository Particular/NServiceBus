﻿namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;
    using Logging;
    using Pipeline;
    using Routing;
    using Transport;
    using Unicast.Queuing;
    using Unicast.Transport;

    class MessageDrivenUnsubscribeTerminator : PipelineTerminator<IUnsubscribeContext>
    {
        public MessageDrivenUnsubscribeTerminator(SubscriptionRouter subscriptionRouter, string replyToAddress, string endpoint, IDispatchMessages dispatcher)
        {
            this.subscriptionRouter = subscriptionRouter;
            this.replyToAddress = replyToAddress;
            this.endpoint = endpoint;
            this.dispatcher = dispatcher;
        }

        protected override Task Terminate(IUnsubscribeContext context)
        {
            var eventType = context.EventType;

            var publisherAddresses = subscriptionRouter.GetAddressesForEventType(eventType);
            if (publisherAddresses.Count == 0)
            {
                throw new Exception( $"No publisher address could be found for message type {eventType}. Ensure the configured publisher endpoint has at least one known instance.");
            }

            var unsubscribeTasks = new List<Task>(publisherAddresses.Count);
            foreach (var publisherAddress in publisherAddresses)
            {
                Logger.Debug("Unsubscribing to " + eventType.AssemblyQualifiedName + " at publisher queue " + publisherAddress);

                var unsubscribeMessage = ControlMessageFactory.Create(MessageIntentEnum.Unsubscribe);

                unsubscribeMessage.Headers[Headers.SubscriptionMessageType] = eventType.AssemblyQualifiedName;
                unsubscribeMessage.Headers[Headers.ReplyToAddress] = replyToAddress;
                unsubscribeMessage.Headers[Headers.SubscriberTransportAddress] = replyToAddress;
                unsubscribeMessage.Headers[Headers.SubscriberEndpoint] = endpoint;
                unsubscribeMessage.Headers[Headers.TimeSent] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);
                unsubscribeMessage.Headers[Headers.NServiceBusVersion] = GitVersionInformation.MajorMinorPatch;

                unsubscribeTasks.Add(SendUnsubscribeMessageWithRetries(publisherAddress, unsubscribeMessage, eventType.AssemblyQualifiedName, context.Extensions));
            }
            return Task.WhenAll(unsubscribeTasks);
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
                    var message = $"Failed to unsubscribe for {messageType} at publisher queue {destination}, reason {ex.Message}";
                    Logger.Error(message, ex);
                    throw new QueueNotFoundException(destination, message, ex);
                }
            }
        }

        readonly string endpoint;
        readonly IDispatchMessages dispatcher;
        readonly string replyToAddress;
        readonly SubscriptionRouter subscriptionRouter;

        static readonly ILog Logger = LogManager.GetLogger<MessageDrivenUnsubscribeTerminator>();

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