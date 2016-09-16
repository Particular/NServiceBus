namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using AcceptanceTesting;

    static class SubscriptionBehaviorExtensions
    {
        public static void OnEndpointSubscribed<TContext>(this EndpointConfiguration configuration, Action<SubscriptionEventArgs, TContext> action) where TContext : ScenarioContext
        {
            configuration.Pipeline.Register(new SubscriptionBehavior<TContext>.Registration("NotifySubscriptionBehavior", builder =>
            {
                var context = builder.Build<TContext>();
                return new SubscriptionBehavior<TContext>(action, context, MessageIntentEnum.Subscribe);
            }));
        }

        public static void OnEndpointUnsubscribed<TContext>(this EndpointConfiguration configuration, Action<SubscriptionEventArgs, TContext> action) where TContext : ScenarioContext
        {
            configuration.Pipeline.Register(new SubscriptionBehavior<TContext>.Registration("NotifyUnsubscriptionBehavior", builder =>
            {
                var context = builder.Build<TContext>();
                return new SubscriptionBehavior<TContext>(action, context, MessageIntentEnum.Unsubscribe);
            }));
        }
    }
}