namespace NServiceBus.Transports.Msmq
{
    using NServiceBus.Config;
    using System;
    using NServiceBus.Settings;
    using Msmq = NServiceBus.Msmq;

    class TimeToBeReceivedOverrideCheck : IWantToRunWhenConfigurationIsComplete
    {
        public void Run()
        {
            var usingMsmq = SettingsHolder.Get<TransportDefinition>("NServiceBus.Transport.SelectedTransport") is Msmq;
            var isTransactional = SettingsHolder.Get<bool>("Transactions.Enabled");

            var messageAuditingConfig = Configure.GetConfigSection<AuditConfig>();
            var auditTTBROverridden = messageAuditingConfig != null && messageAuditingConfig.OverrideTimeToBeReceived > TimeSpan.Zero;

            var unicastBusConfig = Configure.GetConfigSection<UnicastBusConfig>();
            var forwardTTBROverridden = unicastBusConfig != null && unicastBusConfig.TimeToBeReceivedOnForwardedMessages > TimeSpan.Zero;

            TimeToBeRceivedOverrideChecker.Check(usingMsmq, isTransactional, auditTTBROverridden, forwardTTBROverridden);
        }
        
    }
}