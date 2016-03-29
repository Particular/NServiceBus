namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NServiceBus.Pipeline;
    using Transports;

    class SubscriptionBehavior<TContext> : Behavior<IIncomingPhysicalMessageContext> where TContext : ScenarioContext
    {
        public SubscriptionBehavior(Action<SubscriptionEventArgs, TContext> action, TContext scenarioContext)
        {
            this.action = action;
            this.scenarioContext = scenarioContext;
        }

        public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
        {
            await next().ConfigureAwait(false);
            var subscriptionMessageType = GetSubscriptionMessageTypeFrom(context.Message);
            if (subscriptionMessageType != null)
            {
                string returnAddress;
                if (!context.Message.Headers.TryGetValue(Headers.SubscriberTransportAddress, out returnAddress))
                {
                    context.Message.Headers.TryGetValue(Headers.ReplyToAddress, out returnAddress);
                }
                action(new SubscriptionEventArgs
                {
                    MessageType = subscriptionMessageType,
                    SubscriberReturnAddress = returnAddress
                }, scenarioContext);
            }
        }

        static string GetSubscriptionMessageTypeFrom(IncomingMessage msg)
        {
            return (from header in msg.Headers where header.Key == Headers.SubscriptionMessageType select header.Value).FirstOrDefault();
        }

        Action<SubscriptionEventArgs, TContext> action;
        TContext scenarioContext;

        internal class Registration : RegisterStep
        {
            public Registration()
                : base("SubscriptionBehavior", typeof(SubscriptionBehavior<TContext>), "So we can get subscription events")
            {
                InsertBeforeIfExists("ProcessSubscriptionRequests");
            }
        }
    }
}