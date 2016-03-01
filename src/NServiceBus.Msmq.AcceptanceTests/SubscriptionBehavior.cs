namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.Pipeline;

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
            var subscriptionMessageType = GetSubscriptionMessageTypeFrom(context);
            if (subscriptionMessageType != null)
            {
                string returnAddress;
                if (!context.Headers.TryGetValue(Headers.SubscriberTransportAddress, out returnAddress))
                {
                    context.Headers.TryGetValue(Headers.ReplyToAddress, out returnAddress);
                }
                action(new SubscriptionEventArgs
                {
                    MessageType = subscriptionMessageType,
                    SubscriberReturnAddress = returnAddress
                }, scenarioContext);
            }
        }

        static string GetSubscriptionMessageTypeFrom(IIncomingPhysicalMessageContext context)
        {
            return (from header in context.Headers where header.Key == Headers.SubscriptionMessageType select header.Value).FirstOrDefault();
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