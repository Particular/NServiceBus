using NServiceBus.ObjectBuilder;
using NServiceBus.Config;
using Common.Logging;

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
        /// Wraps the given configuration object but stores the same 
        /// builder and configurer properties.
        /// </summary>
        /// <param name="config"></param>
        public void Configure(Configure config)
        {
            Builder = config.Builder;
            Configurer = config.Configurer;

            var cfg = GetConfigSection<MsmqSubscriptionStorageConfig>();

            if (cfg == null)
                Logger.Warn("Could not find configuration section for Msmq Subscription Storage.");

            string q = (cfg != null ? cfg.Queue : "NServiceBus_Subscriptions");

            var storageConfig = Configurer.ConfigureComponent<MsmqSubscriptionStorage>(ComponentCallModelEnum.Singleton);
            storageConfig.ConfigureProperty(s => s.Queue, q);
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof (MsmqSubscriptionStorage));
    }
}
