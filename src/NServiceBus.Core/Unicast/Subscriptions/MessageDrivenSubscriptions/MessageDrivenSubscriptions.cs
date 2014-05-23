namespace NServiceBus.Features
{
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    public class MessageDrivenSubscriptions : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<SubscriptionManager>(DependencyLifecycle.SingleInstance);
        }
    }
}