namespace NServiceBus.Features
{
    using System;
    using System.Configuration;
    using System.Diagnostics;
    using Config;
    using log4net;
    using NServiceBus.Audit;
    using Utils;

    public class Audit : Feature
    {
        static ILog Logger = LogManager.GetLogger(typeof(Audit));

        public override void Initialize()
        {
            base.Initialize();
                
            // Check to see if this entry is specified either in the app.config 
            // (in the new config section or existing unicastBus config section or the registry)
            Address forwardAddress = null;
            var timeToBeReceivedOnForwardedMessages = new TimeSpan();

            // Check the AuditConfig section
            UseSettingsFromAuditConfigIfDefined(ref forwardAddress, ref timeToBeReceivedOnForwardedMessages);

            if (forwardAddress == null)
            {
                // If the audit queue has not been specified either config sections, check the registry
                forwardAddress = ReadAuditQueueNameFromRegistry();
            }

            ThrowIfForwardAddressIsStillUndefined(forwardAddress);

            // Setup the audit queue and the TTR in the MessageAuditer component. This component has
            // already been registered with the bus (ConfigureAudit gets called first, before the feature
            // initialization happens, so we already have an instance of the MessageAuditer)
            Configure.Instance.Configurer
                .ConfigureProperty<MessageAuditer>(p => p.AuditQueue, forwardAddress)
                .ConfigureProperty<MessageAuditer>(t => t.TimeToBeReceivedOnForwardedMessages, timeToBeReceivedOnForwardedMessages);
        }

        public override bool IsEnabledByDefault
        {
            get
            {
                return true;
            }
        }

        Address ReadAuditQueueNameFromRegistry()
        {
            var forwardQueue = RegistryReader<string>.Read("AuditQueue");
            if (string.IsNullOrWhiteSpace(forwardQueue))
            {
                return Address.Undefined;
            }
            
            var forwardAddress = Address.Parse(forwardQueue);

            // Log a warning when running in the debugger to remind user to make sure the 
            // production machine will need to have the required registry setting.
            if (Debugger.IsAttached)
            {
                Logger.Warn("Endpoint auditing is configured using the registry on this machine, please ensure that you either run Set-NServiceBusLocalMachineSettings cmdlet on the target deployment machine or specify the QueueName attribute in the AuditConfig section in your app.config file. To quickly add the AuditConfig section to your app.config, in Package Manager Console type: add-NServiceBusAuditConfig.");
            }
            return forwardAddress;
        }

        void ThrowIfForwardAddressIsStillUndefined(Address forwardAddress)
        {
            // If the forwardAddress is still null, then it hasn't been specified either in the config or registry
            // and the user hasn't turned off the auditing feature -- throw exception in that case.
            if (forwardAddress == Address.Undefined)
            {
                var msg = @"The audit queue has not been configured either in the registry or the app.config file. Either disable auditing feature or configure auditing. 
To disable auditing, add initialization:  Configure.Features.Disable<Audit>(). To configure it in app.config, add the AuditConfig ConfigSection first and then set the QueueName attribute as shown below:
   <AuditConfig QueueName=""audit""/>
To quickly add the AuditConfig section to your app.config, in Package Manager Console type: add-NServiceBusAuditConfig. 
To configure it in registry, run the Set-NServiceBusLocalMachineSettings Powershell cmdlet";
                throw new ConfigurationErrorsException(msg);
            }
        }

        void UseSettingsFromAuditConfigIfDefined(ref Address forwardAddress, ref TimeSpan timeToBeReceivedOnForwardedMessages )
        {
            // Get the auditing configuration - This could be specified either in the new MessageAuditingConfig or still in UnicastBusConfig
            // Check the message auditing config section first for the auditing configuration
            var messageAuditingConfig = Configure.GetConfigSection<AuditConfig>();
            if (messageAuditingConfig != null && !string.IsNullOrWhiteSpace(messageAuditingConfig.QueueName))
            {
                forwardAddress = Address.Parse(messageAuditingConfig.QueueName);
                timeToBeReceivedOnForwardedMessages = messageAuditingConfig.OverrideTimeToBeReceived;
            }
        }

    }
}
