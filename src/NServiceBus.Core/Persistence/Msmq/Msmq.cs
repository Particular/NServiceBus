namespace NServiceBus.Persistence.Legacy
{
    using NServiceBus.Features;

    /// <summary>
    /// Used to enable Msmq persistence.
    /// </summary>
    public class MsmqPersistence : PersistenceDefinition
    {
        internal MsmqPersistence()
        {
            Supports(Storage.Subscriptions, s => s.EnableFeatureByDefault<MsmqSubscriptionPersistence>());
        }
    }
}