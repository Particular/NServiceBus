using NServiceBus.Unicast.Config;

namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods for the purpose of configuring claim flow.
    /// </summary>
    public static class ConfigureClaimFlow
    {
        /// <summary>
        /// Do not flow claims by default
        /// </summary>
        static ConfigureClaimFlow()
        {
            Flow = false;
        }

        /// <summary>
        /// Instructs the bus to flow claims across the nodes.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static ConfigUnicastBus FlowIdentityClaims(this ConfigUnicastBus config)
        {
            return FlowIdentityClaims(config, true);
        }

        /// <summary>
        /// Instructs the bus to flow claims across the nodes.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static ConfigUnicastBus FlowIdentityClaims(this ConfigUnicastBus config, bool value)
        {
            Flow = value;

            return config;
        }

        public static bool Flow { get; private set; }
    }
}