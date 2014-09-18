namespace NServiceBus.Features
{
    using NServiceBus.Faults.InMemory;

    class InMemoryFaultManager : Feature
    {
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<FaultManager>(DependencyLifecycle.SingleInstance);
        }
    }
}