namespace NServiceBus
{
    using NServiceBus.Settings;

    /// <summary>
    ///     Configuration class for durable messaging.
    /// </summary>
    public static class DurableMessagesConfig
    {

        /// <summary>
        /// Configures messages to be guaranteed to be delivered in the event of a computer failure or network problem.
        /// </summary>
        public static void EnableDurableMessages(this ConfigurationBuilder config)
        {
            config.settings.Set("Endpoint.DurableMessages", true);
        }

        /// <summary>
        /// Configures messages that are not guaranteed to be delivered in the event of a computer failure or network problem.
        /// </summary>
        public static void DisableDurableMessages(this ConfigurationBuilder config)
        {
            config.settings.Set("Endpoint.DurableMessages", false);
        }

        internal static bool GetDurableMessagesEnabled(this ReadOnlySettings settings)
        {
            bool durableMessagesEnabled;
            if (settings.TryGet("Endpoint.DurableMessages", out durableMessagesEnabled))
            {
                return durableMessagesEnabled;
            }
            return true;
        }
    }
}
