namespace NServiceBus.Features
{
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    public class MessageDrivenSubscriptions : Feature
    {
        public override void Initialize()
        {
            Configure.Component<MessageDrivenSubscriptionManager>(DependencyLifecycle.SingleInstance);
        }
    }
}