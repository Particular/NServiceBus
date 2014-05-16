namespace NServiceBus.Persistence
{
    using NServiceBus.InMemory.Gateway;
    using NServiceBus.InMemory.Outbox;
    using NServiceBus.InMemory.SagaPersister;
    using NServiceBus.InMemory.SubscriptionStorage;
    using NServiceBus.InMemory.TimeoutPersister;

    class InMemoryPersistence:IConfigurePersistence<InMemory>
    {
        public void Enable(Configure config)
        {
            config.Features.EnableByDefault<InMemorySagaPersistence>();
            config.Features.EnableByDefault<InMemoryTimeoutPersistence>();
            config.Features.EnableByDefault<InMemorySubscriptionPersistence>();
            config.Features.EnableByDefault<InMemoryOutboxPersistence>();
            config.Features.EnableByDefault<InMemoryGatewayPersistence>();
        }
    }

    public class InMemory : PersistenceDefinition
    {
        
    }

}