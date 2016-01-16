namespace NServiceBus
{
    using NServiceBus.Config;
    using System;
    using NServiceBus.ConsistencyGuarantees;
    using NServiceBus.Features;
    using NServiceBus.Settings;
    using NServiceBus.Transports;

    class MsmqTimeToBeReceivedOverrideCheck
    {
        ReadOnlySettings settings;

        public MsmqTimeToBeReceivedOverrideCheck(ReadOnlySettings settings)
        {
            this.settings = settings;
        }
        public StartupCheckResult CheckTimeToBeReceivedOverrides()
        {
            var usingMsmq = settings.Get<TransportDefinition>() is MsmqTransport;
            var isTransactional = settings.GetRequiredTransactionModeForReceives() != TransportTransactionMode.None;
            var outBoxRunning = settings.IsFeatureActive(typeof(Features.Outbox));

            var messageAuditingConfig = settings.GetConfigSection<AuditConfig>();
            var auditTTBROverridden = messageAuditingConfig != null && messageAuditingConfig.OverrideTimeToBeReceived > TimeSpan.Zero;

            var unicastBusConfig = settings.GetConfigSection<UnicastBusConfig>();
            var forwardTTBROverridden = unicastBusConfig != null && unicastBusConfig.TimeToBeReceivedOnForwardedMessages > TimeSpan.Zero;

            return TimeToBeReceivedOverrideChecker.Check(usingMsmq, isTransactional, outBoxRunning, auditTTBROverridden, forwardTTBROverridden);
        }
    }
}