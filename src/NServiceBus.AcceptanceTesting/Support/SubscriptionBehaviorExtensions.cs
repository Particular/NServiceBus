namespace NServiceBus.AcceptanceTesting;

using System;
using Microsoft.Extensions.DependencyInjection;

public static class SubscriptionBehaviorExtensions
{
    extension(EndpointConfiguration configuration)
    {
        public void OnEndpointSubscribed<TContext>(Action<SubscriptionEvent, TContext> action) where TContext : ScenarioContext =>
            configuration.Pipeline.Register("NotifySubscriptionBehavior", builder =>
            {
                var context = builder.GetRequiredService<TContext>();
                return new SubscriptionBehavior<TContext>(action, context, MessageIntent.Subscribe);
            }, "Provides notifications when endpoints subscribe");

        public void OnEndpointUnsubscribed<TContext>(Action<SubscriptionEvent, TContext> action) where TContext : ScenarioContext =>
            configuration.Pipeline.Register("NotifyUnsubscriptionBehavior", builder =>
            {
                var context = builder.GetRequiredService<TContext>();
                return new SubscriptionBehavior<TContext>(action, context, MessageIntent.Unsubscribe);
            }, "Provides notifications when endpoints unsubscribe");
    }
}