using NServiceBus;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.ConsistencyGuarantees;

namespace NServiceBus.ConsistencyGuarantees
{
    using System;
    using NServiceBus.Settings;
    using NServiceBus.Transports;

    /// <summary>
    /// Extension methods to provide access to various consitency releated convenience methods.
    /// </summary>
    public static class ConsistencyGuaranteeSettingsExtensions
    {
        /// <summary>
        /// Returns the transactions required by the transport.
        /// </summary>
        public static TransactionSupport GetRequiredTransactionSupportForReceives(this ReadOnlySettings settings)
        {
            var transportTransactionSupport = settings.Get<TransportDefinition>().GetTransactionSupport();

            ConsistencyGuarantee requestedConsistencyGuarantee;

            //if user haven't asked for a explicit level use what the transport supports
            if (!settings.TryGet(out requestedConsistencyGuarantee))
            {
                return transportTransactionSupport;
            }

            if (requestedConsistencyGuarantee == ConsistencyGuarantee.AtMostOnce)
            {
                return TransactionSupport.None;
            }

            if (requestedConsistencyGuarantee == ConsistencyGuarantee.ExactlyOnce)
            {
                if (transportTransactionSupport != TransactionSupport.Distributed)
                {
                    throw new Exception($"Requested consistency of `{ConsistencyGuarantee.ExactlyOnce}` can't be satisfied since the transport only supports `{transportTransactionSupport}`");
                }

                return TransactionSupport.Distributed;
            }

            if (transportTransactionSupport == TransactionSupport.None)
            {
                throw new Exception($"Requested consistency of `{ConsistencyGuarantee.AtLeastOnce}` can't be satisfied since the transport only supports `{TransactionSupport.None}`");
            }

            if (transportTransactionSupport > TransactionSupport.SingleQueue)
            {
                return TransactionSupport.MultiQueue;
            }

            return TransactionSupport.SingleQueue;
        }
    }
}

/// <summary>
/// Public api for requesting specific consistency guarantees.
/// </summary>
public static class ConsistencyGuaranteeConfigExtensions
{
    /// <summary>
    /// Returns the transactions required by the transport.
    /// </summary>
    public static void RequiredConsistency(this BusConfiguration config, ConsistencyGuarantee guarantee)
    {
        config.GetSettings().Set<ConsistencyGuarantee>(guarantee);
    }
}