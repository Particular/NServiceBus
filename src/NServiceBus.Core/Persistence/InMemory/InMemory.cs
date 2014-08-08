namespace NServiceBus.Persistence
{
    using NServiceBus.Features;

    /// <summary>
    /// Used to enable InMemory persistence.
    /// </summary>
    public class InMemory : PersistenceDefinition
    {
        internal InMemory()
        {
            Supports(Storage.Sagas, s => s.EnableFeatureByDefault<InMemorySagaPersistence>());
            Supports(Storage.Timeouts, s => s.EnableFeatureByDefault<InMemoryTimeoutPersistence>());
            Supports(Storage.Subscriptions, s => s.EnableFeatureByDefault<InMemorySubscriptionPersistence>());
            Supports(Storage.Outbox, s => s.EnableFeatureByDefault<InMemoryOutboxPersistence>());
            Supports(Storage.GatewayDeduplication, s => s.EnableFeatureByDefault<InMemoryGatewayPersistence>());
        }
        
    }
}