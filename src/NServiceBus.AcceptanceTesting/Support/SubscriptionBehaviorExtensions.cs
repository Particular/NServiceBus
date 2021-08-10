namespace NServiceBus.AcceptanceTesting
{
    using System;
    using Microsoft.Extensions.DependencyInjection;

    public static class SubscriptionBehaviorExtensions
    {
        public static void OnEndpointSubscribed<TContext>(this EndpointConfiguration configuration, Action<SubscriptionEvent, TContext> action) where TContext : ScenarioContext
        {
            configuration.Pipeline.Register("NotifySubscriptionBehavior", builder =>
            {
                var context = builder.GetService<TContext>();
                return new SubscriptionBehavior<TContext>(action, context, MessageIntent.Subscribe);
            }, "Provides notifications when endpoints subscribe");
        }

        public static void OnEndpointUnsubscribed<TContext>(this EndpointConfiguration configuration, Action<SubscriptionEvent, TContext> action) where TContext : ScenarioContext
        {
            configuration.Pipeline.Register("NotifyUnsubscriptionBehavior", builder =>
            {
                var context = builder.GetService<TContext>();
                return new SubscriptionBehavior<TContext>(action, context, MessageIntent.Unsubscribe);
            }, "Provides notifications when endpoints unsubscribe");
        }
    }
}