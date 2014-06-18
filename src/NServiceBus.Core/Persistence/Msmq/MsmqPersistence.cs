namespace NServiceBus.Persistence
{
    using System.Collections.Generic;
    using Features;
    using Msmq;

    class MsmqPersistence : IConfigurePersistence<Legacy.Msmq>
    {
        public void Enable(Configure config, List<Storage> storagesToEnable)
        {
            if (storagesToEnable.Contains(Storage.Subscriptions))
                config.Settings.EnableFeatureByDefault<MsmqSubscriptionPersistence>();
        }
    }
}