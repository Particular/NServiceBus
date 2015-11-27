namespace NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions
{
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using NServiceBus.Features;

    class StorageInitializer : Feature
    {
        public StorageInitializer()
        {
            EnableByDefault();
            RegisterStartupTask<CallInit>();
        }

        class CallInit : FeatureStartupTask
        {
            public IInitializableSubscriptionStorage SubscriptionStorage { get; set; }

            protected override Task OnStart(IBusContext context)
            {
                SubscriptionStorage?.Init();
                return TaskEx.Completed;
            }
        }

        protected internal override IReadOnlyCollection<FeatureStartupTask> Setup(FeatureConfigurationContext context)
        {
            return FeatureStartupTask.None;
        }
    }
}