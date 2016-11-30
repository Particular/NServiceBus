namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Logging;
    using Pipeline;
    using Routing;
    using Routing.MessageDrivenSubscriptions;
    using Transport;
    using Unicast.Queuing;
    using Unicast.Transport;

    class MessageDrivenSubscribeConnector : ForkConnector<ISubscribeContext, IUnicastRoutingContext>
    {
        public MessageDrivenSubscribeConnector(Publishers publishers, string subscriberAddress, string subscriberEndpoint)
        {
            this.publishers = publishers;
            this.subscriberAddress = subscriberAddress;
            this.subscriberEndpoint = subscriberEndpoint;
        }

        public override Task Invoke(ISubscribeContext context, Func<Task> next, Func<IUnicastRoutingContext, Task> fork)
        {
            var eventType = context.EventType;

            var routes = publishers.GetPublisherFor(eventType)
                .EnsureNonEmpty(() => $"No publisher address could be found for message type {eventType}.")
                .SelectMany(p => p.CreateRoutes())
                .ToArray();

            if (Logger.IsDebugEnabled)
            {
                var destinations = string.Join(", ", routes.Select(r => r.ToString()));
                Logger.Debug($"Sending subscribe request for {eventType.AssemblyQualifiedName} to {destinations}.");
            }
            var subscriptionMessage = ControlMessageFactory.Create(MessageIntentEnum.Subscribe);

            subscriptionMessage.Headers[Headers.SubscriptionMessageType] = eventType.AssemblyQualifiedName;
            subscriptionMessage.Headers[Headers.ReplyToAddress] = subscriberAddress;
            subscriptionMessage.Headers[Headers.SubscriberTransportAddress] = subscriberAddress;
            subscriptionMessage.Headers[Headers.SubscriberEndpoint] = subscriberEndpoint;
            subscriptionMessage.Headers[Headers.TimeSent] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);
            subscriptionMessage.Headers[Headers.NServiceBusVersion] = GitFlowVersion.MajorMinorPatch;

            return SendSubscribeMessageWithRetries(routes, subscriptionMessage, eventType.AssemblyQualifiedName, context, fork);
        }

        async Task SendSubscribeMessageWithRetries(UnicastRoute[] routes, OutgoingMessage subscriptionMessage, string messageType, ISubscribeContext context, Func<IUnicastRoutingContext, Task> stage, int retriesCount = 0)
        {
            var state = context.Extensions.GetOrCreate<Settings>();
            try
            {
                var downstreamContext = this.CreateUnicastRoutingContext(subscriptionMessage, routes, c => c, context);
                await stage(downstreamContext).ConfigureAwait(false);
            }
            catch (QueueNotFoundException ex)
            {
                if (retriesCount < state.MaxRetries)
                {
                    await Task.Delay(state.RetryDelay).ConfigureAwait(false);
                    await SendSubscribeMessageWithRetries(routes, subscriptionMessage, messageType, context, stage, ++retriesCount).ConfigureAwait(false);
                }
                else
                {
                    string message = $"Failed to subscribe for {messageType} at publisher queue {ex.Queue}, reason {ex.Message}";
                    Logger.Error(message, ex);
                    throw new QueueNotFoundException(ex.Queue, message, ex);
                }
            }
        }

        Publishers publishers;
        string subscriberAddress;
        string subscriberEndpoint;

        static ILog Logger = LogManager.GetLogger<MessageDrivenSubscribeConnector>();

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
