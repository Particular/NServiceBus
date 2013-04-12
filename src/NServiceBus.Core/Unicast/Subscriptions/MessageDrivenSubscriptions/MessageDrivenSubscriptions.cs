namespace NServiceBus.Features
{
    using Config;
    using Transports;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions.SubcriberSideFiltering;

    public class MessageDrivenSubscriptions : IConditionalFeature
    {
        public void Initialize()
        {
            Configure.Component<MessageDrivenSubscriptionManager>(DependencyLifecycle.SingleInstance);
            Configure.Component<FilteringMutator>(DependencyLifecycle.InstancePerCall);
            Configure.Component<SubscriptionPredicatesEvaluator>(DependencyLifecycle.SingleInstance);

            InfrastructureServices.Enable<ISubscriptionStorage>();
        }

        public bool ShouldBeEnabled()
        {
            if (Configure.HasComponent<IManageSubscriptions>())
                return false;

            return true;
        }

        public bool EnabledByDefault()
        {
            return true;
        }
    }
}