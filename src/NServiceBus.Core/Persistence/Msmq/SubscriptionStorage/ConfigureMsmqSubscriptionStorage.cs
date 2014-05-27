namespace NServiceBus
{
    using Config;
    using Logging;
    using Persistence.Msmq.SubscriptionStorage;

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
        public static Configure MsmqSubscriptionStorage(this Configure config)
        {
            return MsmqSubscriptionStorage(config, config.EndpointName);
        }

        /// <summary>
        /// Stores subscription data using MSMQ.
        /// If multiple machines need to share the same list of subscribers,
        /// you should not choose this option - prefer the DbSubscriptionStorage
        /// in that case.
        /// </summary>
        public static Configure MsmqSubscriptionStorage(this Configure config, string endpointName)
        {
            var cfg = config.Settings.GetConfigSection<MsmqSubscriptionStorageConfig>();

            if (cfg == null && string.IsNullOrEmpty(endpointName))
                Logger.Warn("Could not find configuration section for Msmq Subscription Storage and no name was specified for this endpoint. Going to default the subscription queue");

            if (string.IsNullOrEmpty(endpointName))
                endpointName = "NServiceBus";


            Queue = cfg != null ? Address.Parse(cfg.Queue): Address.Parse(endpointName).SubScope("subscriptions");

            var storageConfig = config.Configurer.ConfigureComponent<MsmqSubscriptionStorage>(DependencyLifecycle.SingleInstance);
            storageConfig.ConfigureProperty(s => s.Queue, Queue);

            return config;
        }

        /// <summary>
        /// Queue used to store subscriptions.
        /// </summary>
        public static Address Queue { get; set; }

        static ILog Logger = LogManager.GetLogger(typeof(MsmqSubscriptionStorage));
    }
}
