﻿namespace NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions
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
            public IInitializableSubscriptionStorage SubscriptionStorage { get; set; }

            protected override void OnStart(ISendOnlyBus sendOnlyBus)
            {
                SubscriptionStorage?.Init();
            }
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
        }
    }
}