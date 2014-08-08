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
            Supports(Storage.Sagas, settings => settings.EnableFeatureByDefault<InMemorySagaPersistence>());
            Supports(Storage.Timeouts, settings => settings.EnableFeatureByDefault<InMemoryTimeoutPersistence>());
            Supports(Storage.Subscriptions, settings => settings.EnableFeatureByDefault<InMemorySubscriptionPersistence>());
            Supports(Storage.Outbox, settings => settings.EnableFeatureByDefault<InMemoryOutboxPersistence>());
            Supports(Storage.GatewayDeduplication, settings => settings.EnableFeatureByDefault<InMemoryGatewayPersistence>());
        }
        
    }
}