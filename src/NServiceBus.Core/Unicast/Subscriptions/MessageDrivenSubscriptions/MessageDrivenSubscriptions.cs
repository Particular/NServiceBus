namespace NServiceBus.Features
{
    using Unicast.Subscriptions.MessageDrivenSubscriptions.SubcriberSideFiltering;

    public class MessageDrivenSubscriptions : Feature
    {
        public override void Initialize()
        {
            Configure.Component<FilteringMutator>(DependencyLifecycle.InstancePerCall);
            Configure.Component<SubscriptionPredicatesEvaluator>(DependencyLifecycle.SingleInstance);

        }
    }
}