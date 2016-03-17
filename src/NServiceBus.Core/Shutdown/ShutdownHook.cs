namespace NServiceBus.Features
{
    using NServiceBus;

    class ShutdownHook : Feature
    {
        ShutdownHook()
        {
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<ShutdownDelegateRegistry>(DependencyLifecycle.SingleInstance);
        }
    }
}
