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

    class MessageDrivenSubscribeTerminator : PipelineTerminator<ISubscribeContext>
    {
        public MessageDrivenSubscribeTerminator(SubscriptionRouter subscriptionRouter, string subscriberAddress, string subscriberEndpoint, IMessageDispatcher dispatcher)
        {
            this.subscriptionRouter = subscriptionRouter;
            this.subscriberAddress = subscriberAddress;
            this.subscriberEndpoint = subscriberEndpoint;
            this.dispatcher = dispatcher;
        }

        protected override async Task Terminate(ISubscribeContext context)
        {
            var subscribeTasks = new List<Task>();

            foreach (var eventType in context.EventTypes)
            {
                try
                {
                    var publisherAddresses = subscriptionRouter.GetAddressesForEventType(eventType);
                    if (publisherAddresses.Count == 0)
                    {
                        throw new Exception($"No publisher address could be found for message type '{eventType}'. Ensure that a publisher has been configured for the event type and that the configured publisher endpoint has at least one known instance.");
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

                        subscribeTasks.Add(SendSubscribeMessageWithRetries(publisherAddress, subscriptionMessage, eventType.AssemblyQualifiedName, context.Extensions));
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

        async Task SendSubscribeMessageWithRetries(string destination, OutgoingMessage subscriptionMessage, string messageType, ContextBag context, int retriesCount = 0)
        {
            var state = context.GetOrCreate<Settings>();
            try
            {
                var transportOperation = new TransportOperation(subscriptionMessage, new UnicastAddressTag(destination));
                var transportTransaction = context.GetOrCreate<TransportTransaction>();
                await dispatcher.Dispatch(new TransportOperations(transportOperation), transportTransaction).ConfigureAwait(false);
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

        readonly IMessageDispatcher dispatcher;
        readonly string subscriberAddress;
        readonly string subscriberEndpoint;
        readonly SubscriptionRouter subscriptionRouter;

        static readonly ILog Logger = LogManager.GetLogger<MessageDrivenSubscribeTerminator>();

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
