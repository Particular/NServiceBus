namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods to <see cref="EndpointConfiguration" />.
    /// </summary>
    [ObsoleteEx(TreatAsErrorFromVersion = "8", RemoveInVersion = "9")]
    public static class ConfigureForwarding
    {
        /// <summary>
        /// Sets the address to which received messages will be forwarded.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        /// <param name="address">The address to forward successfully processed messages to.</param>
        [ObsoleteEx(
            Message = "Message forwarding is no longer supported natively by NServiceBus. For auditing messages, use endpointConfiguration.AuditProcessedMessagesTo(address). If true message forwarding capabilities are needed, use a custom pipeline behavior, an example of which can be found in the documentation.",
            TreatAsErrorFromVersion = "8",
            RemoveInVersion = "9")]
        public static void ForwardReceivedMessagesTo(this EndpointConfiguration config, string address)
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNullAndEmpty(nameof(address), address);
            config.Settings.Set(SettingsKey, address);
        }

        internal const string SettingsKey = "forwardReceivedMessagesTo";
    }
}