namespace NServiceBus.ConsistencyGuarantees
{
    using System;
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

            if (!settings.TryGet<ReceiveConfiguration>(out var receiveConfiguration))
            {
                throw new InvalidOperationException("Receive transaction mode isn't available since this endpoint is configured to run in send only mode.");
            }

            return receiveConfiguration.TransactionMode;
        }
    }
}