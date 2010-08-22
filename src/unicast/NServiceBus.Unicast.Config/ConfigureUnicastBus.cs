using NServiceBus.Config;
using NServiceBus.Unicast.Config;

namespace NServiceBus
{
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
            Instance = new ConfigUnicastBus();
            Instance.Configure(config);

            return Instance;
        }

        internal static ConfigUnicastBus Instance { get; private set; }
    }

    class EnsureLoadMessageHandlersWasCalled : INeedInitialization
    {
        void INeedInitialization.Init()
        {
            if (!ConfigureUnicastBus.Instance.LoadMessageHandlersCalled)
                ConfigureUnicastBus.Instance.LoadMessageHandlers();
        }
    }
}
