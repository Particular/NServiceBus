namespace NServiceBus
{
    using Unicast.Config;

    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    public static class ConfigureUnicastBus
    {
        /// <summary>
        /// Use unicast messaging (your best option on nServiceBus right now).
        /// </summary>
        public static ConfigUnicastBus UnicastBus(this Configure config)
        {
            if (Instance == null)
            {
                Instance = new ConfigUnicastBus(config.TypesToScan);
                Instance.Configure(config);
            }

            return Instance;
        }

        internal static ConfigUnicastBus Instance { get; private set; }
    }

    class EnsureLoadMessageHandlersWasCalled : INeedInitialization
    {
        public void Init(Configure config)
        {
            if (ConfigureUnicastBus.Instance != null)
            {
                if (!ConfigureUnicastBus.Instance.LoadMessageHandlersCalled)
                {
                    ConfigureUnicastBus.Instance.LoadMessageHandlers();
                }
            }
        }
    }
}
