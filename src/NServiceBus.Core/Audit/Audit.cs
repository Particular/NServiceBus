namespace NServiceBus.Features
{
    using System;
    using System.Diagnostics;
    using Config;
    using Logging;
    using NServiceBus.Audit;
    using Unicast.Queuing.Installers;
    using Utils;

    public class Audit : Feature
    {
        static ILog Logger = LogManager.GetLogger<Audit>();

        public override void Initialize(Configure config)
        {
            // If Audit feature is enabled and the value not specified via config and instead specified in the registry:
            // Log a warning when running in the debugger to remind user to make sure the 
            // production machine will need to have the required registry setting.
            if (Debugger.IsAttached && GetAuditQueueAddressFromAuditConfig(config) == Address.Undefined)
            {
                Logger.Warn("Endpoint auditing is configured using the registry on this machine, please ensure that you either run Set-NServiceBusLocalMachineSettings cmdlet on the target deployment machine or specify the QueueName attribute in the AuditConfig section in your app.config file. To quickly add the AuditConfig section to your app.config, in Package Manager Console type: add-NServiceBusAuditConfig.");
            }


            config.Pipeline.Register<AuditBehavior.Registration>();

            

            var auditQueue = GetConfiguredAuditQueue(config);

            config.Configurer.ConfigureComponent<AuditQueueCreator>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(t => t.AuditQueue, auditQueue);

            var behaviorConfig = config.Configurer.ConfigureComponent<AuditBehavior>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.AuditQueue, auditQueue);


            var messageAuditingConfig = config.GetConfigSection<AuditConfig>();
            if (messageAuditingConfig != null && messageAuditingConfig.OverrideTimeToBeReceived > TimeSpan.Zero)
            {
                behaviorConfig.ConfigureProperty(t => t.TimeToBeReceivedOnForwardedMessages, messageAuditingConfig.OverrideTimeToBeReceived);
            }
        }

        public override bool IsEnabledByDefault
        {
            get { return true; }
        }

        public override bool ShouldBeEnabled(Configure config)
        {
            // Check to see if this entry is specified either in the AuditConfig section in app.config 
            // or configured in the the registry. If neither place has the value set, then turn off auditing
            // to be backwards compatible.
            return GetConfiguredAuditQueue(config) != Address.Undefined;
        }

        Address GetConfiguredAuditQueue(Configure config)
        {
            var auditAddress = GetAuditQueueAddressFromAuditConfig(config);
            
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

        Address  GetAuditQueueAddressFromAuditConfig(Configure config)
        {
            var messageAuditingConfig = config.GetConfigSection<AuditConfig>();
            if (messageAuditingConfig != null && !string.IsNullOrWhiteSpace(messageAuditingConfig.QueueName))
            {
                return Address.Parse(messageAuditingConfig.QueueName);
            }
            return Address.Undefined;
        }

        static ILog Logger = LogManager.GetLogger(typeof(Audit));

      
    }
}