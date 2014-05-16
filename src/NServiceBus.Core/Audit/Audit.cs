namespace NServiceBus.Features
{
    using System;
    using System.Diagnostics;
    using Config;
    using Logging;
    using NServiceBus.Audit;
    using Utils;

    public class Audit : Feature
    {
        static ILog Logger = LogManager.GetLogger(typeof(Audit));

        public override void Initialize(Configure config)
        {
            // If Audit feature is enabled and the value not specified via config and instead specified in the registry:
            // Log a warning when running in the debugger to remind user to make sure the 
            // production machine will need to have the required registry setting.
            if (Debugger.IsAttached && GetAuditQueueAddressFromAuditConfig() == Address.Undefined)
            {
                Logger.Warn("Endpoint auditing is configured using the registry on this machine, please ensure that you either run Set-NServiceBusLocalMachineSettings cmdlet on the target deployment machine or specify the QueueName attribute in the AuditConfig section in your app.config file. To quickly add the AuditConfig section to your app.config, in Package Manager Console type: add-NServiceBusAuditConfig.");
            }
   
            // Setup the audit queue and the TTR in the MessageAuditer component. This component has
            // already been registered with the bus (InitMessageAuditer gets called first, before the feature
            // initialization happens, so we already have an instance of the MessageAuditer)
            config.Configurer
                .ConfigureProperty<MessageAuditer>(p => p.AuditQueue, GetConfiguredAuditQueue())
                .ConfigureProperty<MessageAuditer>(t => t.TimeToBeReceivedOnForwardedMessages, GetTimeToBeReceivedFromAuditConfig());
        }

        public override bool IsEnabledByDefault
        {
            get
            {
                // Check to see if this entry is specified either in the AuditConfig section in app.config 
                // or configured in the the registry. If neither place has the value set, then turn off auditing
                // to be backwards compatible.
                return GetConfiguredAuditQueue() != Address.Undefined;
            }
        }

        Address GetConfiguredAuditQueue()
        {
            var auditAddress = GetAuditQueueAddressFromAuditConfig();
            
            if (auditAddress == Address.Undefined)
            {
                // Check to see if the audit queue has been specified either in the registry as a global setting
                auditAddress = ReadAuditQueueNameFromRegistry();
            }
            return auditAddress;

        }

        Address ReadAuditQueueNameFromRegistry()
        {
            var forwardQueue = RegistryReader<string>.Read("AuditQueue");
            if (string.IsNullOrWhiteSpace(forwardQueue))
            {
                return Address.Undefined;
            }            
            return Address.Parse(forwardQueue);
        }

        Address  GetAuditQueueAddressFromAuditConfig()
        {
            var messageAuditingConfig = Configure.GetConfigSection<AuditConfig>();
            if (messageAuditingConfig != null && !string.IsNullOrWhiteSpace(messageAuditingConfig.QueueName))
            {
                return Address.Parse(messageAuditingConfig.QueueName);
            }
            return Address.Undefined;
        }


        TimeSpan GetTimeToBeReceivedFromAuditConfig()
        {
            var messageAuditingConfig = Configure.GetConfigSection<AuditConfig>();
            if (messageAuditingConfig != null)
            {   
                return messageAuditingConfig.OverrideTimeToBeReceived;
            }
            return new TimeSpan();
        }
    }
}
