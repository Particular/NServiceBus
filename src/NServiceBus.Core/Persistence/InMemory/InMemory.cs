namespace NServiceBus.Persistence
{
    public class InMemory : PersistenceDefinition
    {
        public InMemory()
        {
            HasGatewaysStorage = true;
            HasOutboxStorage = true;
            HasSagaStorage = true;
            HasSubscriptionStorage = true;
            HasTimeoutStorage = true;
        }
    }
}