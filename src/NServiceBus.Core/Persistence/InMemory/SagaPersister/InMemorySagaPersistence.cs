namespace NServiceBus.InMemory.SagaPersister
{
    using Features;

    public class InMemorySagaPersistence:Feature
    {
        public InMemorySagaPersistence()
        {
            DependsOn<Sagas>();
        }

        public override void Initialize(Configure config)
        {
            config.Configurer.ConfigureComponent<InMemorySagaPersister>(DependencyLifecycle.SingleInstance);
        }
    }
}