namespace NServiceBus
{
    using System;
    using Config;
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
            //TODO fix.
            //var isTransactional = settings.GetRequiredTransactionModeForReceives() != TransportTransactionMode.None;
            var outBoxRunning = settings.IsFeatureActive(typeof(Features.Outbox));

            var messageAuditingConfig = settings.GetConfigSection<AuditConfig>();
            var auditTTBROverridden = messageAuditingConfig != null && messageAuditingConfig.OverrideTimeToBeReceived > TimeSpan.Zero;

            var unicastBusConfig = settings.GetConfigSection<UnicastBusConfig>();
            var forwardTTBROverridden = unicastBusConfig != null && unicastBusConfig.TimeToBeReceivedOnForwardedMessages > TimeSpan.Zero;

            //TODO use isTransactional
            return TimeToBeReceivedOverrideChecker.Check(usingMsmq, true, outBoxRunning, auditTTBROverridden, forwardTTBROverridden);
        }

        ReadOnlySettings settings;
    }
}