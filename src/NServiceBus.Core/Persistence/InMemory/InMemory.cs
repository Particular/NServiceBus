namespace NServiceBus.Persistence
{
    public class InMemory : PersistenceDefinition
    {
        public InMemory()
        {
            HasGatewayStorage = true;
            HasOutboxStorage = true;
            HasSagaStorage = true;
            HasSubscriptionStorage = true;
            HasTimeoutStorage = true;
        }
    }
}