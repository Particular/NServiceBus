namespace NServiceBus.Persistence.Legacy
{
    public class Msmq : PersistenceDefinition
    {
        public Msmq()
        {
            Supports(Storage.Subscriptions);
        }
    }
}