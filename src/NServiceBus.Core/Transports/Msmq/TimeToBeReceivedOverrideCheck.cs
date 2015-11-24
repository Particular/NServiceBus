namespace NServiceBus.Transports.Msmq
{
    using NServiceBus.Config;
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Features;
    using NServiceBus.Settings;

    class TimeToBeReceivedOverrideCheck : FeatureStartupTask
    {
        ReadOnlySettings settings;

        public TimeToBeReceivedOverrideCheck(ReadOnlySettings settings)
        {
            this.settings = settings;
        }
        protected override Task OnStart(IBusContext context)
        {
            var usingMsmq = settings.Get<TransportDefinition>() is MsmqTransport;
            var isTransactional = settings.Get<bool>("Transactions.Enabled");
            var outBoxRunning = settings.IsFeatureActive(typeof(Outbox));

            var messageAuditingConfig = settings.GetConfigSection<AuditConfig>();
            var auditTTBROverridden = messageAuditingConfig != null && messageAuditingConfig.OverrideTimeToBeReceived > TimeSpan.Zero;

            var unicastBusConfig = settings.GetConfigSection<UnicastBusConfig>();
            var forwardTTBROverridden = unicastBusConfig != null && unicastBusConfig.TimeToBeReceivedOnForwardedMessages > TimeSpan.Zero;

            TimeToBeReceivedOverrideChecker.Check(usingMsmq, isTransactional, outBoxRunning, auditTTBROverridden, forwardTTBROverridden);

            return TaskEx.Completed;
        }
    }
}