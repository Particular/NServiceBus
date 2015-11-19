namespace NServiceBus.Transports.Msmq
{
    using NServiceBus.Config;
    using System;
    
    class TimeToBeReceivedOverrideCheck : IWantToRunWhenConfigurationIsComplete
    {
        public void Run(Configure config)
        {
            var usingMsmq  = config.Settings.Get<TransportDefinition>() is MsmqTransport;
            var isTransactional = config.Settings.Get<bool>("Transactions.Enabled");
            var outBoxRunning = config.Settings.GetOrDefault<bool>("NServiceBus.Features.Outbox");

            var messageAuditingConfig = config.Settings.GetConfigSection<AuditConfig>();
            var auditTTBROverridden = messageAuditingConfig != null && messageAuditingConfig.OverrideTimeToBeReceived > TimeSpan.Zero;

            var unicastBusConfig = config.Settings.GetConfigSection<UnicastBusConfig>();
            var forwardTTBROverridden = unicastBusConfig != null && unicastBusConfig.TimeToBeReceivedOnForwardedMessages > TimeSpan.Zero;
            
            TimeToBeRceivedOverrideChecker.Check(usingMsmq, isTransactional, outBoxRunning, auditTTBROverridden, forwardTTBROverridden);
        }
    }
} 
