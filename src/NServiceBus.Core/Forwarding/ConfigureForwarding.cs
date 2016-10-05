namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods to <see cref="EndpointConfiguration" />.
    /// </summary>
    public static class ConfigureForwarding
    {
        /// <summary>
        /// Sets the address to which received messages will be forwarded.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        /// <param name="address">The address to forward successfully processed messages to.</param>
        public static void ForwardReceivedMessagesTo(this EndpointConfiguration config, string address)
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNullAndEmpty(nameof(address), address);
            config.Settings.Set(SettingsKey, address);
        }

        internal const string SettingsKey = "forwardReceivedMessagesTo";
    }
}