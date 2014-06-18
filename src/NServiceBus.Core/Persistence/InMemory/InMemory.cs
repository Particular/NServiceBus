namespace NServiceBus.Persistence
{
    public class InMemory : PersistenceDefinition
    {
        public InMemory()
        {
            Supports(Storage.GatewayDeduplication);
            Supports(Storage.Timeouts);
            Supports(Storage.Sagas);
            Supports(Storage.Subscriptions);
            Supports(Storage.Outbox);
        }
    }
}