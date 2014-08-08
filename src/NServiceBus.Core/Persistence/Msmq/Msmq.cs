namespace NServiceBus.Persistence.Legacy
{
    using NServiceBus.Features;
    using NServiceBus.Persistence.Msmq;

    /// <summary>
    /// Used to enable Msmq persistence.
    /// </summary>
    public class Msmq : PersistenceDefinition
    {
        internal Msmq()
        {
            Supports(Storage.Subscriptions, s => s.EnableFeatureByDefault<MsmqSubscriptionPersistence>());
        }
    }
}