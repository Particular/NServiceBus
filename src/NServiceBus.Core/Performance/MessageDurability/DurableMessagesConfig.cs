namespace NServiceBus
{
    using System;
    using Settings;

    /// <summary>
    /// Configuration class for durable messaging.
    /// </summary>
    public static class DurableMessagesConfig
    {
        /// <summary>
        /// Configures messages to be guaranteed to be delivered in the event of a computer failure or network problem.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        public static void EnableDurableMessages(this EndpointConfiguration config)
        {
            Guard.AgainstNull(nameof(config), config);
            config.Settings.Set("Endpoint.DurableMessages", true);
        }

        /// <summary>
        /// Configures messages that are not guaranteed to be delivered in the event of a computer failure or network problem.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        public static void DisableDurableMessages(this EndpointConfiguration config)
        {
            Guard.AgainstNull(nameof(config), config);
            config.Settings.Set("Endpoint.DurableMessages", false);
        }

        /// <summary>
        /// Returns whether durable messages are on or off.
        /// </summary>
        public static bool DurableMessagesEnabled(this ReadOnlySettings settings)
        {
            Guard.AgainstNull(nameof(settings), settings);
            bool durableMessagesEnabled;
            if (settings.TryGet("Endpoint.DurableMessages", out durableMessagesEnabled))
            {
                return durableMessagesEnabled;
            }
            return true;
        }

        /// <summary>
        /// Returns whether durable messages are on or off.
        /// </summary>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "DurableMessagesEnabled")]
        public static bool DurableMessagesEnabled(this Configure config)
        {
            throw new NotImplementedException();
        }
    }
}