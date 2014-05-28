namespace NServiceBus.Persistence
{
    using Features;
    using NServiceBus.InMemory.Gateway;
    using NServiceBus.InMemory.Outbox;
    using NServiceBus.InMemory.SagaPersister;
    using NServiceBus.InMemory.SubscriptionStorage;
    using NServiceBus.InMemory.TimeoutPersister;

    class InMemoryPersistence:IConfigurePersistence<InMemory>
    {
        public void Enable(Configure config)
        {
            config.Settings.EnableFeatureByDefault<InMemorySagaPersistence>();
            config.Settings.EnableFeatureByDefault<InMemoryTimeoutPersistence>();
            config.Settings.EnableFeatureByDefault<InMemorySubscriptionPersistence>();
            config.Settings.EnableFeatureByDefault<InMemoryOutboxPersistence>();
            config.Settings.EnableFeatureByDefault<InMemoryGatewayPersistence>();
        }
    }

    public class InMemory : PersistenceDefinition
    {
        
    }

}