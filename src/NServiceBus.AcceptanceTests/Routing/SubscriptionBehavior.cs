namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.Pipeline;

    static class SubscriptionBehaviorExtensions
    {
        public static void OnEndpointSubscribed<TContext>(this BusConfiguration b, Action<SubscriptionEventArgs, TContext> action) where TContext : ScenarioContext
        {
            b.Pipeline.Register<SubscriptionBehavior<TContext>.Registration>();

            b.RegisterComponents(c => c.ConfigureComponent(builder =>
            {
                var context = builder.Build<TContext>();
                return new SubscriptionBehavior<TContext>(action, context);
            }, DependencyLifecycle.InstancePerCall));
        }
    }

    class SubscriptionBehavior<TContext> : PhysicalMessageProcessingStageBehavior where TContext : ScenarioContext
    {
        Action<SubscriptionEventArgs, TContext> action;
        TContext scenarioContext;

        public SubscriptionBehavior(Action<SubscriptionEventArgs, TContext> action, TContext scenarioContext)
        {
            this.action = action;
            this.scenarioContext = scenarioContext;
        }

        public override async Task Invoke(Context context, Func<Task> next)
        {
            await next().ConfigureAwait(false);
            var subscriptionMessageType = GetSubscriptionMessageTypeFrom(context.GetPhysicalMessage());
            if (subscriptionMessageType != null)
            {
                action(new SubscriptionEventArgs
                {
                    MessageType = subscriptionMessageType,
                    SubscriberReturnAddress = context.GetPhysicalMessage().ReplyToAddress
                }, scenarioContext);
            }
        }

        static string GetSubscriptionMessageTypeFrom(TransportMessage msg)
        {
            return (from header in msg.Headers where header.Key == Headers.SubscriptionMessageType select header.Value).FirstOrDefault();
        }

        internal class Registration : RegisterStep
        {
            public Registration()
                : base("SubscriptionBehavior", typeof(SubscriptionBehavior<TContext>), "So we can get subscription events")
            {
                InsertBefore("ProcessSubscriptionRequests");
            }
        }
    }
}