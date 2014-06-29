namespace NServiceBus.Persistence
{
    /// <summary>
    /// Used to enable InMemory persistence <see cref="IConfigurePersistence{T}"/>
    /// </summary>
    public class InMemory : PersistenceDefinition
    {
        internal InMemory()
        {
            Supports(Storage.GatewayDeduplication);
            Supports(Storage.Timeouts);
            Supports(Storage.Sagas);
            Supports(Storage.Subscriptions);
            Supports(Storage.Outbox);
        }
    }
}