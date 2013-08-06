namespace NServiceBus.Features
{
    using System;
    using System.Configuration;
    using System.Diagnostics;
    using System.Messaging;
    using System.Text;
    using Config;
    using log4net;
    using NServiceBus.Audit;
    using Utils;

    public class Audit : Feature
    {
        readonly static ILog Logger = LogManager.GetLogger(typeof(Audit));

        public override void Initialize()
        {
            base.Initialize();

            // Check to see if this entry is specified either in the app.config 
            // (in the new config section or existing unicastbus config section or the registry)
            Address forwardAddress = null;
            var timeToBeReceivedOnForwardedMessages = new TimeSpan();
            
            // Get the auditing configuration - This could be specified either in the new MessageAuditingConfig or still in UnicastBusConfig
            var messageAuditingConfig = Configure.GetConfigSection<AuditConfig>();
            var unicastConfig = Configure.GetConfigSection<UnicastBusConfig>();

            // Check the message auditing config section first for the auditing configuration
            if (messageAuditingConfig != null && !string.IsNullOrWhiteSpace(messageAuditingConfig.ForwardReceivedMessagesTo))
            {
                forwardAddress = Address.Parse(messageAuditingConfig.ForwardReceivedMessagesTo);
                timeToBeReceivedOnForwardedMessages = messageAuditingConfig.TimeToBeReceivedOnForwardedMessages;
            }
            
            // Check to see if the audit values are also being defined in the UnicastBusConfig section. If so raise a warning.
            if (unicastConfig != null)
            {
                if (!string.IsNullOrWhiteSpace(unicastConfig.ForwardReceivedMessagesTo))
                {
                    // Log a warning
                    Logger.Warn("Please use AuditConfig instead of UnicastBusConfig to specify audit related configuration such as ForwardReceivedMessagesTo. To quickly add the AuditConfig section, in Package Manager Console type: add-NServiceBusAuditConfig. The audit related configuration will be removed from UnicastBusConfig in v5.0");

                    if (forwardAddress == null)
                    {
                        Logger.Warn("Using the audit configuration values defined in the UnicastBusConfig for now");
                        forwardAddress = Address.Parse(unicastConfig.ForwardReceivedMessagesTo);
                        timeToBeReceivedOnForwardedMessages = unicastConfig.TimeToBeReceivedOnForwardedMessages;
                    }
                    else
                        Logger.Warn("Since AuditConfig section was found, using the values defined in the AuditConfig instead of the values defined in UnicastBusConfig section.");
                }
            }

            // If the audit queue has not been specified either config sections, check the registry
            if (forwardAddress == null)
            {
                var forwardQueue = RegistryReader<string>.Read("AuditQueue");
                if (!string.IsNullOrWhiteSpace(forwardQueue))
                {
                    forwardAddress = Address.Parse(forwardQueue);

                    // Log a warning when running in the debugger to remind user to make sure the 
                    // production machine will need to have the required registry setting.
                    if (Debugger.IsAttached)
                    {
                        Logger.Warn("Endpoint auditing is configured using the registry on this machine, please ensure that you either run Set-NServiceBusLocalMachineSettings cmdlet on the target deployment machine or specify the ForwardMessagesReceivedTo attribute in the UnicastBusConfig section in your app.config file.");
                    }
                }
            }

            // If the forwardAddress is still null, then it hasn't been specified either in the config or registry
            // and the user hasn't turned off the auditing feature -- throw exception in that case.
            if (forwardAddress == null || forwardAddress == Address.Undefined)
            {
                var sb = new StringBuilder();
                sb.AppendLine("The audit queue has not been configured either in the registry or the app.config file. ");
                sb.AppendLine("Either disable auditing feature or configure auditing");
                sb.AppendLine("To disable auditing, add initialization:  Configure.Features.Disable<Audit>()");
                sb.AppendLine("To configure it in app.config, add the AuditConfig section and set the ForwardReceivedMessagesTo attribute as shown below:");
                sb.AppendLine("<AuditConfig ForwardReceivedMessagesTo=\"audit\"/>");
                sb.AppendLine("To configure it in registry, run the Set-NServiceBusLocalMachineSettings Powershell cmdlet");
                sb.AppendLine();
                throw new ConfigurationErrorsException(sb.ToString());
            }

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
    }
}
