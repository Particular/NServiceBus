namespace NServiceBus
{
    using System.Threading.Tasks;
    using NServiceBus.Features;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

    class StorageInitializer : Feature
    {
        public StorageInitializer()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<CallInit>(DependencyLifecycle.SingleInstance);

            context.RegisterStartupTask(b => b.Build<CallInit>());
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
    }
}