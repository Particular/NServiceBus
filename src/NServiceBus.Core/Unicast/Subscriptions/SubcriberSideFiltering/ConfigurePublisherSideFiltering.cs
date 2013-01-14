namespace NServiceBus.Unicast.Subscriptions.SubcriberSideFiltering
{
    class ConfigurePublisherSideFiltering:INeedInitialization
    {
        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<FilteringMutator>(DependencyLifecycle.InstancePerCall);
            Configure.Instance.Configurer.ConfigureComponent<SubscriptionPredicatesEvaluator>(DependencyLifecycle.SingleInstance);
        }
    }
}