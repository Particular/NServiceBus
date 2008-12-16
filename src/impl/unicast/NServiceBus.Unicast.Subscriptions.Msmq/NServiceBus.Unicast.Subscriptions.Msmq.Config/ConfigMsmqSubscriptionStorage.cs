
using System.Configuration;
using ObjectBuilder;
using NServiceBus.Config;

namespace NServiceBus.Unicast.Subscriptions.Msmq.Config
{
    public class ConfigMsmqSubscriptionStorage : Configure
    {
        public ConfigMsmqSubscriptionStorage() : base() { }

        public void Configure(Configure config)
        {
            this.Builder = config.Builder;
            this.Configurer = config.Configurer;

            MsmqSubscriptionStorageConfig cfg =
                ConfigurationManager.GetSection("MsmqSubscriptionStorageConfig") as MsmqSubscriptionStorageConfig;

            if (cfg == null)
                throw new ConfigurationErrorsException("Could not find configuration section for Msmq Subscription Storage.");

            MsmqSubscriptionStorage storage = this.Configurer.ConfigureComponent<MsmqSubscriptionStorage>(ComponentCallModelEnum.Singleton);
            storage.Queue = cfg.Queue;
        }
    }
}
