using System.Threading;
using NServiceBus.Transports;

namespace NServiceBus
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

    class MigrationSubscribeTerminator : PipelineTerminator<ISubscribeContext>
    {
        public MigrationSubscribeTerminator(IManageSubscriptions subscriptionManager, SubscriptionRouter subscriptionRouter, IDispatchMessages dispatcher, string subscriberAddress, string subscriberEndpoint)
        {
            this.subscriptionManager = subscriptionManager;
            this.subscriptionRouter = subscriptionRouter;
            this.dispatcher = dispatcher;
            this.subscriberAddress = subscriberAddress;
            this.subscriberEndpoint = subscriberEndpoint;
        }

        protected override async Task Terminate(ISubscribeContext context)
        {
            var eventType = context.EventType;

            await subscriptionManager.Subscribe(eventType, context.Extensions).ConfigureAwait(false);

            var publisherAddresses = subscriptionRouter.GetAddressesForEventType(eventType);
            if (publisherAddresses.Count == 0)
            {
                return;
            }

            var subscribeTasks = new List<Task>(publisherAddresses.Count);
            foreach (var publisherAddress in publisherAddresses)
            {
                Logger.Debug($"Subscribing to {eventType.AssemblyQualifiedName} at publisher queue {publisherAddress}");

                var subscriptionMessage = ControlMessageFactory.Create(MessageIntentEnum.Subscribe);

                subscriptionMessage.Headers[Headers.SubscriptionMessageType] = eventType.AssemblyQualifiedName;
                subscriptionMessage.Headers[Headers.ReplyToAddress] = subscriberAddress;
                subscriptionMessage.Headers[Headers.SubscriberTransportAddress] = subscriberAddress;
                subscriptionMessage.Headers[Headers.SubscriberEndpoint] = subscriberEndpoint;
                subscriptionMessage.Headers[Headers.TimeSent] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);
                subscriptionMessage.Headers[Headers.NServiceBusVersion] = GitVersionInformation.MajorMinorPatch;

                subscribeTasks.Add(SendSubscribeMessageWithRetries(publisherAddress, subscriptionMessage, eventType.AssemblyQualifiedName, context.Extensions));
            }

            await Task.WhenAll(subscribeTasks).ConfigureAwait(false);
        }

        async Task SendSubscribeMessageWithRetries(string destination, OutgoingMessage subscriptionMessage, string messageType, ContextBag context, int retriesCount = 0)
        {
            var state = context.GetOrCreate<MessageDrivenSubscribeTerminator.Settings>();
            try
            {
                var transportOperation = new TransportOperation(subscriptionMessage, new UnicastAddressTag(destination), context.GetTransportProperties().Properties);
                var transportTransaction = context.GetOrCreate<TransportTransaction>();
                await dispatcher.Dispatch(new TransportOperations(transportOperation), transportTransaction, CancellationToken.None).ConfigureAwait(false);
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
                    var message = $"Failed to subscribe to {messageType} at publisher queue {destination}, reason {ex.Message}";
                    Logger.Error(message, ex);
                    throw new QueueNotFoundException(destination, message, ex);
                }
            }
        }

        readonly SubscriptionRouter subscriptionRouter;

        readonly string subscriberAddress;
        readonly string subscriberEndpoint;
        readonly IDispatchMessages dispatcher;

        readonly IManageSubscriptions subscriptionManager;
        static ILog Logger = LogManager.GetLogger<MigrationSubscribeTerminator>();
    }
}