namespace NServiceBus.Persistence.Legacy
{
    /// <summary>
    /// Used to enable Msmq persistence <see cref="IConfigurePersistence{T}"/>
    /// </summary>
    public class Msmq : PersistenceDefinition
    {
        internal Msmq()
        {
            Supports(Storage.Subscriptions);
        }
    }
}