namespace NServiceBus.Transport.Msmq.AcceptanceTests
{
    using System;
    using AcceptanceTesting;

    static class SubscriptionBehaviorExtensions
    {
        public static void OnEndpointSubscribed<TContext>(this EndpointConfiguration b, Action<SubscriptionEventArgs, TContext> action) where TContext : ScenarioContext
        {
            b.Pipeline.Register(builder =>
            {
                var context = builder.Build<TContext>();
                return new SubscriptionBehavior<TContext>(action, context);
            }, "Provides notifications when endpoints subscribe");
        }
    }
}