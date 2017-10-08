namespace NServiceBus.ConsistencyGuarantees
{
    using Settings;

    /// <summary>
    /// Extension methods to provide access to various consistency related convenience methods.
    /// </summary>
    public static class TransactionModeSettingsExtensions
    {
        /// <summary>
        /// Returns the transactions required by the transport.
        /// </summary>
        public static TransportTransactionMode GetRequiredTransactionModeForReceives(this ReadOnlySettings settings)
        {
            Guard.AgainstNull(nameof(settings), settings);

            return settings.Get<ReceiveConfiguration>().TransactionMode;
        }
    }
}