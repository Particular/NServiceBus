namespace NServiceBus.Features
{
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    /// <summary>
    /// Used to configure Message Driven Subscriptions
    /// </summary>
    public class MessageDrivenSubscriptions : Feature
    {

        internal MessageDrivenSubscriptions()
        {
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<SubscriptionManager>(DependencyLifecycle.SingleInstance);
        }
    }
}