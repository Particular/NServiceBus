
using System.Configuration;
using ObjectBuilder;
using NServiceBus.Config;

namespace NServiceBus.Unicast.Subscriptions.Msmq.Config
{
    public class ConfigMsmqSubscriptionStorage : NServiceBus.Config.Configure
    {
        public ConfigMsmqSubscriptionStorage() : base() { }

        public void Configure(IBuilder builder)
        {
            this.builder = builder;

            MsmqSubscriptionStorageConfig cfg =
                ConfigurationManager.GetSection("MsmqSubscriptionStorageConfig") as MsmqSubscriptionStorageConfig;

            if (cfg == null)
                throw new ConfigurationErrorsException("Could not find configuration section for Msmq Subscription Storage.");

            MsmqSubscriptionStorage storage = builder.ConfigureComponent<MsmqSubscriptionStorage>(ComponentCallModelEnum.Singleton);
            storage.Queue = cfg.Queue;
        }
    }
}
