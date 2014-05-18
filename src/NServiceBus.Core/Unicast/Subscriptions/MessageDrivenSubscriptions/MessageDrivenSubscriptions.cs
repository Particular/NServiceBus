namespace NServiceBus.Features
{
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    public class MessageDrivenSubscriptions : Feature
    {
        public override void Initialize(Configure config)
        {
            config.Configurer.ConfigureComponent<MessageDrivenSubscriptionManager>(DependencyLifecycle.SingleInstance);
        }
    }
}