namespace NServiceBus.Features
{
    using Config;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions.SubcriberSideFiltering;

    public class MessageDrivenSubscriptions : IFeature
    {
        public void Initalize()
        {
            Configure.Component<MessageDrivenSubscriptionManager>(DependencyLifecycle.SingleInstance);
            Configure.Component<FilteringMutator>(DependencyLifecycle.InstancePerCall);
            Configure.Component<SubscriptionPredicatesEvaluator>(DependencyLifecycle.SingleInstance);

            InfrastructureServices.Enable<ISubscriptionStorage>();
        }
    }
}