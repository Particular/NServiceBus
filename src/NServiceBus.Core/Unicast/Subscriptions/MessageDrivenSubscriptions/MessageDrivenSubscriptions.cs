namespace NServiceBus.Features
{
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    /// <summary>
    /// Message Driven Subscriptions
    /// </summary>
    public class MessageDrivenSubscriptions : Feature
    {

        internal MessageDrivenSubscriptions()
        {
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<SubscriptionManager>(DependencyLifecycle.SingleInstance);
        }
    }
}