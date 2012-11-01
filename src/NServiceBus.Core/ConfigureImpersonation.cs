namespace NServiceBus
{
    using Unicast.Config;

    /// <summary>
    /// Contains extension methods for the purpose of configuring impersonation.
    /// </summary>
    public static class ConfigureImpersonation
    {
        /// <summary>
        /// Impersonate by default, otherwise this configuration would not be backward compatible
        /// </summary>
        static ConfigureImpersonation()
        {
            Impersonate = true;
        }

        /// <summary>
        /// Instructs the bus to run the processing of messages being handled
        /// under the permissions of the sender of the message.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static ConfigUnicastBus ImpersonateSender(this ConfigUnicastBus config, bool value)
        {
            Impersonate = value;

            return config;
        }

        public static bool Impersonate { get; private set; }
    }
}