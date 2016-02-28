namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using NServiceBus.AcceptanceTesting;

    static class SubscriptionBehaviorExtensions
    {
        public static void OnEndpointSubscribed<TContext>(this EndpointConfiguration b, Action<SubscriptionEventArgs, TContext> action) where TContext : ScenarioContext
        {
            b.Pipeline.Register<SubscriptionBehavior<TContext>.Registration>();

            b.RegisterComponents(c => c.ConfigureComponent(builder =>
            {
                var context = builder.Build<TContext>();
                return new SubscriptionBehavior<TContext>(action, context);
            }, DependencyLifecycle.InstancePerCall));
        }
    }
}