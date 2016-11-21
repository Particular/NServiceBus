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

    class MessageDrivenUnsubscribeConnector : ForkConnector<IUnsubscribeContext, IUnicastRoutingContext>
    {
        public MessageDrivenUnsubscribeConnector(Publishers publishers, string replyToAddress, string endpoint)
        {
            this.publishers = publishers;
            this.replyToAddress = replyToAddress;
            this.endpoint = endpoint;
        }

        public override Task Invoke(IUnsubscribeContext context, Func<Task> next, Func<IUnicastRoutingContext, Task> fork)
        {
            var eventType = context.EventType;

            var routes = publishers.GetPublisherFor(eventType)
                .EnsureNonEmpty(() => $"No publisher address could be found for message type {eventType}.")
                .SelectMany(p => p.CreateRoutes())
                .ToArray();

            if (Logger.IsDebugEnabled)
            {
                var destinations = string.Join(", ", routes.Select(r => r.ToString()));
                Logger.Debug($"Sending unsubscribe request for {eventType.AssemblyQualifiedName} to {destinations}.");
            }

            var unsubscribeMessage = ControlMessageFactory.Create(MessageIntentEnum.Unsubscribe);

            unsubscribeMessage.Headers[Headers.SubscriptionMessageType] = eventType.AssemblyQualifiedName;
            unsubscribeMessage.Headers[Headers.ReplyToAddress] = replyToAddress;
            unsubscribeMessage.Headers[Headers.SubscriberTransportAddress] = replyToAddress;
            unsubscribeMessage.Headers[Headers.SubscriberEndpoint] = endpoint;
            unsubscribeMessage.Headers[Headers.TimeSent] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);
            unsubscribeMessage.Headers[Headers.NServiceBusVersion] = GitFlowVersion.MajorMinorPatch;

            return SendUnsubscribeMessageWithRetries(routes, unsubscribeMessage, eventType.AssemblyQualifiedName, context, fork);
        }

        async Task SendUnsubscribeMessageWithRetries(UnicastRoute[] routes, OutgoingMessage unsubscribeMessage, string messageType, IUnsubscribeContext context, Func<IUnicastRoutingContext, Task> stage, int retriesCount = 0)
        {
            var state = context.Extensions.GetOrCreate<Settings>();
            try
            {
                var downstreamContext = this.CreateUnicastRoutingContext(unsubscribeMessage, routes, c => c, context);
                await stage(downstreamContext).ConfigureAwait(false);
            }
            catch (QueueNotFoundException ex)
            {
                if (retriesCount < state.MaxRetries)
                {
                    await Task.Delay(state.RetryDelay).ConfigureAwait(false);
                    await SendUnsubscribeMessageWithRetries(routes, unsubscribeMessage, messageType, context, stage, ++retriesCount).ConfigureAwait(false);
                }
                else
                {
                    string message = $"Failed to unsubscribe for {messageType} at publisher queue {ex.Queue}, reason {ex.Message}";
                    Logger.Error(message, ex);
                    throw new QueueNotFoundException(ex.Queue, message, ex);
                }
            }
        }

        string endpoint;
        Publishers publishers;
        string replyToAddress;


        static ILog Logger = LogManager.GetLogger<MessageDrivenUnsubscribeConnector>();

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