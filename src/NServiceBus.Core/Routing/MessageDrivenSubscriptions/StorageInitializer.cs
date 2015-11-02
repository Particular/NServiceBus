namespace NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions
{
    using System.Threading.Tasks;
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

            protected override Task OnStart(IBusContext context)
            {
                SubscriptionStorage?.Init();
                return TaskEx.Completed;
            }
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
        }
    }
}