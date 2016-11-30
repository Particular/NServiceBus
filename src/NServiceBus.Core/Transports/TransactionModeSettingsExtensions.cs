namespace NServiceBus.ConsistencyGuarantees
{
    using System;
    using Features;

    /// <summary>
    /// Extension methods to provide access to various consistency related convenience methods.
    /// </summary>
    public static class TransactionModeSettingsExtensions
    {
        /// <summary>
        /// Returns the transactions required by the transport.
        /// </summary>
        public static TransportTransactionMode GetRequiredTransactionModeForReceives(this FeatureConfigurationContext context)
        {
            var transportTransactionSupport = context.Transport.TransportInfrastructure.TransactionMode;

            TransportTransactionMode requestedTransportTransactionMode;

            //if user haven't asked for a explicit level use what the transport supports
            if (!context.Settings.TryGet(out requestedTransportTransactionMode))
            {
                return transportTransactionSupport;
            }

            if (requestedTransportTransactionMode > transportTransactionSupport)
            {
                throw new Exception($"Requested transaction mode `{requestedTransportTransactionMode}` can't be satisfied since the transport only supports `{transportTransactionSupport}`");
            }

            return requestedTransportTransactionMode;
        }
    }
}