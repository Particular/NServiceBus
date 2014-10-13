namespace NServiceBus.Features
{
    class ErrorSubscribers : Feature
    {
        public ErrorSubscribers()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<BusNotifications>(DependencyLifecycle.SingleInstance);
        }
    }
}
