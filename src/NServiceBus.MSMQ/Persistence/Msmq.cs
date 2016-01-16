﻿namespace NServiceBus.Persistence.Legacy
{
    using NServiceBus.Features;

    /// <summary>
    /// Used to enable Msmq persistence.
    /// </summary>
    public class MsmqPersistence : PersistenceDefinition
    {
        internal MsmqPersistence()
        {
            Supports<StorageType.Subscriptions>(s => s.EnableFeatureByDefault<MsmqSubscriptionPersistence>());
        }
    }
}