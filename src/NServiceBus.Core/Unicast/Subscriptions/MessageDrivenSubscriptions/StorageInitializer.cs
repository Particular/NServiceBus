namespace NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions
{
    using NServiceBus.Features;

    internal class StorageInitializer : Feature
    {
        public StorageInitializer()
        {
            EnableByDefault();
            RegisterStartupTask<CallInit>();
        }

        class CallInit : FeatureStartupTask
        {
            public ISubscriptionStorage SubscriptionStorage { get; set; }

            protected override void OnStart()
            {
                if (SubscriptionStorage != null)
                {
                    SubscriptionStorage.Init();
                }
            }
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            
        }
    }
}