namespace NServiceBus.Persistence
{
    using System.Collections.Generic;
    using Features;

    class InMemoryPersistence:IConfigurePersistence<InMemory>
    {
        public void Enable(Configure config, List<Storage> storagesToEnable)
        {
            if (storagesToEnable.Contains(Storage.Sagas))
                config.Settings.EnableFeatureByDefault<InMemorySagaPersistence>();    
            
            if (storagesToEnable.Contains(Storage.Timeouts))
                config.Settings.EnableFeatureByDefault<InMemoryTimeoutPersistence>();

            if (storagesToEnable.Contains(Storage.Subscriptions))
                config.Settings.EnableFeatureByDefault<InMemorySubscriptionPersistence>();

            if (storagesToEnable.Contains(Storage.Outbox))
                config.Settings.EnableFeatureByDefault<InMemoryOutboxPersistence>();

            if (storagesToEnable.Contains(Storage.GatewayDeduplication))
                config.Settings.EnableFeatureByDefault<InMemoryGatewayPersistence>();
        }
    }
}