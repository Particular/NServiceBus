using Common.Logging;
using NServiceBus.Config;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast.Subscriptions.Msmq;

namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    public static class ConfigureMsmqSubscriptionStorage
    {
        /// <summary>
        /// Stores subscription data using MSMQ.
        /// If multiple machines need to share the same list of subscribers,
        /// you should not choose this option - prefer the DbSubscriptionStorage
        /// in that case.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure MsmqSubscriptionStorage(this Configure config)
        {
            var cfg = Configure.GetConfigSection<MsmqSubscriptionStorageConfig>();

            if (cfg == null)
                Logger.Warn("Could not find configuration section for Msmq Subscription Storage.");

            Queue = (cfg != null ? cfg.Queue : "NServiceBus_Subscriptions");

            var storageConfig = config.Configurer.ConfigureComponent<MsmqSubscriptionStorage>(DependencyLifecycle.SingleInstance);
            storageConfig.ConfigureProperty(s => s.Queue, Queue);

            return config;
        }

        /// <summary>
        /// Queue used to store subscriptions.
        /// </summary>
        public static string Queue { get; set; }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(MsmqSubscriptionStorage));
    }
}
