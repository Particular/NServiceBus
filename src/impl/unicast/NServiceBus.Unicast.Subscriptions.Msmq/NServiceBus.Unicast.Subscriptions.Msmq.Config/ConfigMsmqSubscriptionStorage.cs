using System.Configuration;
using NServiceBus.ObjectBuilder;
using NServiceBus.Config;

namespace NServiceBus.Unicast.Subscriptions.Msmq.Config
{
    /// <summary>
    /// Extends the base Configure class with MsmqSubscriptionStorage specific methods.
    /// Reads administrator set values from the MsmqSubscriptionStorageConfig section
    /// of the app.config.
    /// </summary>
    public class ConfigMsmqSubscriptionStorage : Configure
    {
        /// <summary>
        /// Constructor needed since we have an additional constructor.
        /// </summary>
        public ConfigMsmqSubscriptionStorage() : base() { }

        /// <summary>
        /// Wraps the given configuration object but stores the same 
        /// builder and configurer properties.
        /// </summary>
        /// <param name="config"></param>
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
