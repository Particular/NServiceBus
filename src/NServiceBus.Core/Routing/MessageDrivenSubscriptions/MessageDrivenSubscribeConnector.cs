namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Logging;
    using Pipeline;
    using Routing;
    using Transport;
    using Unicast.Queuing;
    using Unicast.Transport;

    class MessageDrivenSubscribeConnector : StageConnector<ISubscribeContext, IDispatchContext>
    {
        public MessageDrivenSubscribeConnector (SubscriptionRouter subscriptionRouter, string subscriberAddress, string subscriberEndpoint)
        {
            this.subscriptionRouter = subscriptionRouter;
            this.subscriberAddress = subscriberAddress;
            this.subscriberEndpoint = subscriberEndpoint;
        }

        public override Task Invoke(ISubscribeContext context, Func<IDispatchContext, Task> stage)
        {
            var eventType = context.EventType;

            var publisherAddresses = subscriptionRouter.GetAddressesForEventType(eventType);
            if (publisherAddresses.Count == 0)
            {
                throw new Exception($"No publisher address could be found for message type '{eventType}'. Ensure that a publisher has been configured for the event type and that the configured publisher endpoint has at least one known instance.");
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

                subscribeTasks.Add(SendSubscribeMessageWithRetries(publisherAddress, subscriptionMessage, eventType.AssemblyQualifiedName, context, stage));
            }
            return Task.WhenAll(subscribeTasks);
        }

        async Task SendSubscribeMessageWithRetries(string destination, OutgoingMessage subscriptionMessage, string messageType, ISubscribeContext context, Func<IDispatchContext, Task> stage, int retriesCount = 0)
        {
            var state = context.Extensions.GetOrCreate<Settings>();
            try
            {
                var transportOperation = new TransportOperation(subscriptionMessage, new UnicastAddressTag(destination));
                await stage(this.CreateDispatchContext(new[] { transportOperation }, context)).ConfigureAwait(false);
            }
            catch (QueueNotFoundException ex)
            {
                if (retriesCount < state.MaxRetries)
                {
                    await Task.Delay(state.RetryDelay).ConfigureAwait(false);
                    await SendSubscribeMessageWithRetries(destination, subscriptionMessage, messageType, context, stage, ++retriesCount).ConfigureAwait(false);
                }
                else
                {
                    var message = $"Failed to subscribe to {messageType} at publisher queue {destination}, reason {ex.Message}";
                    Logger.Error(message, ex);
                    throw new QueueNotFoundException(destination, message, ex);
                }
            }
        }
        
        readonly string subscriberAddress;
        readonly string subscriberEndpoint;
        readonly SubscriptionRouter subscriptionRouter;

        static readonly ILog Logger = LogManager.GetLogger<MessageDrivenSubscribeConnector>();

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
