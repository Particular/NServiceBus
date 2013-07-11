namespace NServiceBus
{
    using Config;
    using Unicast.Config;

    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    public static class ConfigureUnicastBus
    {
        /// <summary>
        /// Use unicast messaging (your best option on nServiceBus right now).
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static ConfigUnicastBus UnicastBus(this Configure config)
        {
            if (Instance == null)
            {
                Instance = new ConfigUnicastBus();
                Instance.Configure(config);
            }

            return Instance;
        }

        /// <summary>
        /// Return Timeout Manager Address. Uses "TimeoutManagerAddress" parameter form config file if defined, if not, uses "EndpointName.Timeouts".
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Address GetTimeoutManagerAddress(this Configure config)
        {
            var unicastConfig = Configure.GetConfigSection<UnicastBusConfig>();

            if (unicastConfig != null && !string.IsNullOrWhiteSpace(unicastConfig.TimeoutManagerAddress))
            {
                return Address.Parse(unicastConfig.TimeoutManagerAddress);
            }

            return config.GetMasterNodeAddress().SubScope("Timeouts");
        }

        internal static ConfigUnicastBus Instance { get; private set; }
    }

    class EnsureLoadMessageHandlersWasCalled : INeedInitialization
    {
        public void Init()
        {
            if (ConfigureUnicastBus.Instance != null)
                if (!ConfigureUnicastBus.Instance.LoadMessageHandlersCalled)
                    ConfigureUnicastBus.Instance.LoadMessageHandlers();
        }
    }
}
