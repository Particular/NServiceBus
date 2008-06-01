
using System.Configuration;
using ObjectBuilder;

namespace NServiceBus.Unicast.Subscriptions.Msmq.Config
{
    public class ConfigMsmqSubscriptionStorage
    {
        public ConfigMsmqSubscriptionStorage(IBuilder builder)
        {
            MsmqSubscriptionStorageConfig cfg =
                ConfigurationManager.GetSection("MsmqSubscriptionStorageConfig") as MsmqSubscriptionStorageConfig;

            if (cfg == null)
                throw new ConfigurationErrorsException("Could not find configuration section for Msmq Subscription Storage.");

            MsmqSubscriptionStorage storage = builder.ConfigureComponent<MsmqSubscriptionStorage>(ComponentCallModelEnum.Singleton);
            storage.Queue = cfg.Queue;
        }
    }
}
