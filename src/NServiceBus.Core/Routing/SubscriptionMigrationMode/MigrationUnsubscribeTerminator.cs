namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Logging;
    using NServiceBus.Unicast.Transport;
    using Pipeline;
    using Routing;
    using Transport;
    using Unicast.Messages;
    using Unicast.Queuing;

    class MigrationUnsubscribeTerminator : PipelineTerminator<IUnsubscribeContext>
    {
        public MigrationUnsubscribeTerminator(ISubscriptionManager subscriptionManager, MessageMetadataRegistry messageMetadataRegistry, SubscriptionRouter subscriptionRouter, IMessageDispatcher dispatcher, ReceiveAddresses receiveAddresses, string endpoint)
        {
            this.subscriptionManager = subscriptionManager;
            this.messageMetadataRegistry = messageMetadataRegistry;
            this.subscriptionRouter = subscriptionRouter;
            this.dispatcher = dispatcher;
            replyToAddress = receiveAddresses.MainReceiveAddress;
            this.endpoint = endpoint;
        }

        protected override async Task Terminate(IUnsubscribeContext context)
        {
            var eventType = context.EventType;
            var eventMetadata = messageMetadataRegistry.GetMessageMetadata(eventType);

            await subscriptionManager.Unsubscribe(eventMetadata, context.Extensions, context.CancellationToken).ConfigureAwait(false);


            var publisherAddresses = subscriptionRouter.GetAddressesForEventType(eventType);
            if (publisherAddresses.Count == 0)
            {
                return;
            }

            var unsubscribeTasks = new List<Task>(publisherAddresses.Count);
            foreach (var publisherAddress in publisherAddresses)
            {
                Logger.Debug("Unsubscribing to " + eventType.AssemblyQualifiedName + " at publisher queue " + publisherAddress);

                var unsubscribeMessage = ControlMessageFactory.Create(MessageIntent.Unsubscribe);

                unsubscribeMessage.Headers[Headers.SubscriptionMessageType] = eventType.AssemblyQualifiedName;
                unsubscribeMessage.Headers[Headers.ReplyToAddress] = replyToAddress;
                unsubscribeMessage.Headers[Headers.SubscriberTransportAddress] = replyToAddress;
                unsubscribeMessage.Headers[Headers.SubscriberEndpoint] = endpoint;
                unsubscribeMessage.Headers[Headers.TimeSent] = DateTimeOffsetHelper.ToWireFormattedString(DateTimeOffset.UtcNow);
                unsubscribeMessage.Headers[Headers.NServiceBusVersion] = VersionInformation.MajorMinorPatch;

                unsubscribeTasks.Add(SendUnsubscribeMessageWithRetries(publisherAddress, unsubscribeMessage, eventType.AssemblyQualifiedName, context.Extensions, 0, context.CancellationToken));
            }

            await Task.WhenAll(unsubscribeTasks).ConfigureAwait(false);
        }

        async Task SendUnsubscribeMessageWithRetries(string destination, OutgoingMessage unsubscribeMessage, string messageType, ContextBag context, int retriesCount, CancellationToken cancellationToken)
        {
            var state = context.GetOrCreate<MessageDrivenUnsubscribeTerminator.Settings>();
            try
            {
                var transportOperation = new TransportOperation(unsubscribeMessage, new UnicastAddressTag(destination));
                var transportTransaction = context.GetOrCreate<TransportTransaction>();
                await dispatcher.Dispatch(new TransportOperations(transportOperation), transportTransaction, cancellationToken).ConfigureAwait(false);
            }
            catch (QueueNotFoundException ex)
            {
                if (retriesCount < state.MaxRetries)
                {
                    await Task.Delay(state.RetryDelay, cancellationToken).ConfigureAwait(false);
                    await SendUnsubscribeMessageWithRetries(destination, unsubscribeMessage, messageType, context, ++retriesCount, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var message = $"Failed to unsubscribe for {messageType} at publisher queue {destination}, reason {ex.Message}";
                    Logger.Error(message, ex);
                    throw new QueueNotFoundException(destination, message, ex);
                }
            }
        }

        readonly ISubscriptionManager subscriptionManager;
        readonly MessageMetadataRegistry messageMetadataRegistry;

        readonly string endpoint;
        readonly IMessageDispatcher dispatcher;
        readonly string replyToAddress;
        readonly SubscriptionRouter subscriptionRouter;
        static ILog Logger = LogManager.GetLogger<MigrationUnsubscribeTerminator>();
    }
}