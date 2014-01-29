namespace NServiceBus.Features
{
    using Unicast.Subscriptions.MessageDrivenSubscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions.SubcriberSideFiltering;

    public class MessageDrivenSubscriptions : Feature
    {
        public override void Initialize()
        {
            Configure.Component<FilteringMutator>(DependencyLifecycle.InstancePerCall);
            Configure.Component<SubscriptionPredicatesEvaluator>(DependencyLifecycle.SingleInstance);

            var masterNodeAddress = Configure.Instance.GetMasterNodeAddress();
            Configure.Component<MessageDrivenSubscriptionManager>(
             DependencyLifecycle.SingleInstance)
             .ConfigureProperty(r => r.DistributorDataAddress, masterNodeAddress);

        }
    }
}