namespace NServiceBus.Features
{

    class UnicastBus : Feature
    {
        internal UnicastBus()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<BusNotifications>(DependencyLifecycle.SingleInstance);
        }
    }
}