namespace NServiceBus
{
    using System;
    using Config;
    using ConsistencyGuarantees;
    using Features;
    using Settings;
    using Transport;

    class MsmqTimeToBeReceivedOverrideCheck
    {
        public MsmqTimeToBeReceivedOverrideCheck(ReadOnlySettings settings)
        {
            this.settings = settings;
        }

        public StartupCheckResult CheckTimeToBeReceivedOverrides()
        {
            var usingMsmq = settings.Get<TransportDefinition>() is MsmqTransport;
            var isTransactional = settings.GetRequiredTransactionModeForReceives() != TransportTransactionMode.None;
            var outBoxRunning = settings.IsFeatureActive(typeof(Features.Outbox));

            var messageAuditingConfig = settings.GetOrDefault<AuditConfigReader.Result>();
            var auditTTBROverridden = messageAuditingConfig?.TimeToBeReceived > TimeSpan.Zero;

            return TimeToBeReceivedOverrideChecker.Check(usingMsmq, isTransactional, outBoxRunning, auditTTBROverridden);
        }

        ReadOnlySettings settings;
    }
}