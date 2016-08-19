namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Messaging;
    using System.Transactions;
    using Routing;
    using Settings;

    /// <summary>
    /// Adds extensions methods to <see cref="TransportExtensions{T}" /> for configuration purposes.
    /// </summary>
    public static class MsmqConfigurationExtensions
    {
        /// <summary>
        /// Set a delegate to use for applying the <see cref="Message.Label" /> property when sending a message.
        /// </summary>
        /// <remarks>
        /// This delegate will be used for all valid messages sent via MSMQ.
        /// This includes, not just standard messages, but also Audits, Errors and all control messages.
        /// In some cases it may be useful to use the <see cref="Headers.ControlMessageHeader" /> key to determine if a message is
        /// a control message.
        /// The only exception to this rule is received messages with corrupted headers. These messages will be forwarded to the
        /// error queue with no label applied.
        /// </remarks>
        public static TransportExtensions<MsmqTransport> ApplyLabelToMessages(this TransportExtensions<MsmqTransport> transportExtensions, Func<IReadOnlyDictionary<string, string>, string> labelGenerator)
        {
            Guard.AgainstNull(nameof(labelGenerator), labelGenerator);
            transportExtensions.Settings.Set("msmqLabelGenerator", labelGenerator);
            return transportExtensions;
        }

        /// <summary>
        /// Allows to change the transaction isolation level and timeout for the `TransactionScope` used to receive messages.
        /// </summary>
        /// <remarks>
        /// If not specified the default transaction timeout of the machine will be used and the isolation level will be set to
        /// `ReadCommited`.
        /// </remarks>
        public static TransportExtensions<MsmqTransport> TransactionScopeOptions(this TransportExtensions<MsmqTransport> transportExtensions, TimeSpan? timeout = null, IsolationLevel? isolationLevel = null)
        {
            transportExtensions.Settings.Set<MsmqScopeOptions>(new MsmqScopeOptions(timeout, isolationLevel));
            return transportExtensions;
        }

        /// <summary>
        /// Sets a distribution strategy for a given endpoint.
        /// </summary>
        /// <param name="config">Config object.</param>
        /// <param name="endpoint">Endpoint to configure the strategy for.</param>
        /// <param name="distributionStrategyFactory">The factory producing instances of a distribution strategy. The is one strategy instance per each endpoint per operation type (send, publish).</param>
        public static void SetMessageDistributionStrategy(this RoutingSettings<MsmqTransport> config, string endpoint, Func<ReadOnlySettings, DistributionStrategy> distributionStrategyFactory)
        {
            DistributionPolicy distributionPolicy;
            var settings = config.Settings;
            if (!settings.TryGet(out distributionPolicy))
            {
                distributionPolicy = new DistributionPolicy(settings);
                settings.Set<DistributionPolicy>(distributionPolicy);
            }
            distributionPolicy.SetDistributionStrategy(endpoint, distributionStrategyFactory);
        }

        /// <summary>
        /// Returns the configuration options for the file based instance mapping file.
        /// </summary>
        public static InstanceMappingFileSettings InstanceMappingFile(this RoutingSettings<MsmqTransport> config)
        {
            return new InstanceMappingFileSettings(config.Settings);
        }

        /// <summary>
        /// Overrides the default address translation rule "endpoint.quailfier-id@machine".
        /// </summary>
        /// <param name="config">Config object.</param>
        /// <param name="translationRule">New translation rule.</param>
        public static TransportExtensions<MsmqTransport> OverrideAddressTranslation(this TransportExtensions<MsmqTransport> config, Func<LogicalAddress, string> translationRule)
        {
            config.Settings.Set("NServiceBus.Transports.MSMQ.AddressTranslationRule", translationRule);
            return config;
        }
    }
}