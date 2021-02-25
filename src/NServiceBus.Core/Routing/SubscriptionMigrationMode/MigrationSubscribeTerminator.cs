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

    class MigrationSubscribeTerminator : PipelineTerminator<ISubscribeContext>
    {
        public MigrationSubscribeTerminator(ISubscriptionManager subscriptionManager,
            MessageMetadataRegistry messageMetadataRegistry, SubscriptionRouter subscriptionRouter,
            IMessageDispatcher dispatcher, string subscriberAddress, string subscriberEndpoint)
        {
            this.subscriptionManager = subscriptionManager;
            this.messageMetadataRegistry = messageMetadataRegistry;
            this.subscriptionRouter = subscriptionRouter;
            this.dispatcher = dispatcher;
            this.subscriberAddress = subscriberAddress;
            this.subscriberEndpoint = subscriberEndpoint;
        }

        protected override async Task Terminate(ISubscribeContext context)
        {
            var eventMetadata = new MessageMetadata[context.EventTypes.Length];
            for (int i = 0; i < context.EventTypes.Length; i++)
            {
                eventMetadata[i] = messageMetadataRegistry.GetMessageMetadata(context.EventTypes[i]);
            }
            try
            {
                await subscriptionManager.SubscribeAll(eventMetadata, context.Extensions, context.CancellationToken).ConfigureAwait(false);
            }
            catch (AggregateException e)
            {
                if (context.Extensions.TryGet<bool>(MessageSession.SubscribeAllFlagKey, out var flag) && flag)
                {
                    throw;
                }

                // if this is called from Subscribe, rethrow the expected single exception
                throw e.InnerException ?? e;
            }

            var subscribeTasks = new List<Task>();
            foreach (var eventType in context.EventTypes)
            {
                try
                {
                    var publisherAddresses = subscriptionRouter.GetAddressesForEventType(eventType);
                    if (publisherAddresses.Count == 0)
                    {
                        continue;
                    }

                    foreach (var publisherAddress in publisherAddresses)
                    {
                        Logger.Debug($"Subscribing to {eventType.AssemblyQualifiedName} at publisher queue {publisherAddress}");

                        var subscriptionMessage = ControlMessageFactory.Create(MessageIntentEnum.Subscribe);

                        subscriptionMessage.Headers[Headers.SubscriptionMessageType] = eventType.AssemblyQualifiedName;
                        subscriptionMessage.Headers[Headers.ReplyToAddress] = subscriberAddress;
                        subscriptionMessage.Headers[Headers.SubscriberTransportAddress] = subscriberAddress;
                        subscriptionMessage.Headers[Headers.SubscriberEndpoint] = subscriberEndpoint;
                        subscriptionMessage.Headers[Headers.TimeSent] = DateTimeOffsetHelper.ToWireFormattedString(DateTimeOffset.UtcNow);
                        subscriptionMessage.Headers[Headers.NServiceBusVersion] = GitVersionInformation.MajorMinorPatch;

                        subscribeTasks.Add(SendSubscribeMessageWithRetries(publisherAddress, subscriptionMessage, eventType.AssemblyQualifiedName, context.Extensions, 0, context.CancellationToken));
                    }
                }
                catch (Exception e)
                {
                    subscribeTasks.Add(Task.FromException(e));
                }
            }

            var t = Task.WhenAll(subscribeTasks);
            try
            {
                await t.ConfigureAwait(false);
            }
            catch (Exception)
            {
                // if subscribing via SubscribeAll, throw an AggregateException
                if (context.Extensions.TryGet<bool>(MessageSession.SubscribeAllFlagKey, out var flag) && flag)
                {
                    throw t.Exception;
                }

                // otherwise throw the first exception to not change exception behavior when calling subscribe.
                throw;
            }
        }

        async Task SendSubscribeMessageWithRetries(string destination, OutgoingMessage subscriptionMessage, string messageType, ContextBag context, int retriesCount, CancellationToken cancellationToken = default)
        {
            var state = context.GetOrCreate<MessageDrivenSubscribeTerminator.Settings>();
            try
            {
                var transportOperation = new TransportOperation(subscriptionMessage, new UnicastAddressTag(destination));
                var transportTransaction = context.GetOrCreate<TransportTransaction>();
                await dispatcher.Dispatch(new TransportOperations(transportOperation), transportTransaction, cancellationToken).ConfigureAwait(false);
            }
            catch (QueueNotFoundException ex)
            {
                if (retriesCount < state.MaxRetries)
                {
                    await Task.Delay(state.RetryDelay, cancellationToken).ConfigureAwait(false);
                    await SendSubscribeMessageWithRetries(destination, subscriptionMessage, messageType, context, ++retriesCount, cancellationToken).ConfigureAwait(false);
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
        readonly IMessageDispatcher dispatcher;

        readonly ISubscriptionManager subscriptionManager;
        readonly MessageMetadataRegistry messageMetadataRegistry;
        static ILog Logger = LogManager.GetLogger<MigrationSubscribeTerminator>();
    }
}