namespace NServiceBus.ConsistencyGuarantees
{
    using System;
    using Transport;
    using Settings;

    /// <summary>
    /// Extension methods to provide access to various consistency related convenience methods.
    /// </summary>
    public static class TransactionModeSettingsExtensions
    {
        /// <summary>
        /// Returns the transactions required by the transport.
        /// </summary>
        public static TransportTransactionMode GetRequiredTransactionModeForReceives(this IReadOnlySettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            var transportDefinition = settings.Get<TransportDefinition>();

            return transportDefinition.TransportTransactionMode;
        }
    }
}