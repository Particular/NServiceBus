namespace NServiceBus.Features
{
    using Config;
    using Unicast.Publishing;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    public class MessageDrivenPublisher : IFeature
    {
        public void Initialize()
        {
            Configure.Component<StorageDrivenPublisher>(DependencyLifecycle.InstancePerCall);

            InfrastructureServices.Enable<ISubscriptionStorage>();
        }
    }
}