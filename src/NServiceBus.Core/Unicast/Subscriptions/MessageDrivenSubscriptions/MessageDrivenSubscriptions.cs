namespace NServiceBus.Features
{
    using Config;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions.SubcriberSideFiltering;

    public class MessageDrivenSubscriptions : IFeature, IFinalizeConfiguration
    {
        public void FinalizeConfiguration()
        {
            if (!Feature.IsEnabled<MessageDrivenSubscriptions>())
                return;

            Configure.Component<MessageDrivenSubscriptionManager>(DependencyLifecycle.SingleInstance);
            Configure.Component<FilteringMutator>(DependencyLifecycle.InstancePerCall);
            Configure.Component<SubscriptionPredicatesEvaluator>(DependencyLifecycle.SingleInstance);
       
            InfrastructureServices.Enable<ISubscriptionStorage>();
        }
    }
}