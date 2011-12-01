using System;
using System.Configuration;
using NServiceBus.Config;
using NServiceBus.Faults;
using NServiceBus.Faults.Forwarder;
using NServiceBus.ObjectBuilder;
using Common.Logging;
using NServiceBus.Utils;

namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods to NServiceBus.Configure
    /// </summary>
    public static class ConfigureFaultsForwarder
    {
        /// <summary>
        /// Forward messages that have repeatedly failed to another endpoint.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure MessageForwardingInCaseOfFault(this Configure config)
        {
            var section = Configure.GetConfigSection<MessageForwardingInCaseOfFaultConfig>();
            if (section == null)
            {
                Logger.Warn("Could not find configuration section 'MessageForwardingInCaseOfFaultConfig'. Going to try to find the error queue defined in 'MsmqTransportConfig'.");

                var msmq = Configure.GetConfigSection<MsmqTransportConfig>();
                if (msmq == null)
                    throw new ConfigurationErrorsException("Could not find backup configuration section 'MsmqTransportConfig' in order to locate the error queue.");


                ErrorQueue = Address.Parse(msmq.ErrorQueue);
            }
            else
                ErrorQueue =  Address.Parse(section.ErrorQueue);

			if(ErrorQueue == Address.Undefined)
				throw new ConfigurationErrorsException("Faults forwarding requires a error queue to be specified. Please add a 'MessageForwardingInCaseOfFaultConfig' section to your app.config");
             
            config.Configurer.ConfigureComponent<FaultManager>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(fm => fm.ErrorQueue, ErrorQueue);

            return config;
        }

        /// <summary>
        /// The queue to which to forward errors.
        /// </summary>
        public static Address ErrorQueue { get; set; }

        private static ILog Logger = LogManager.GetLogger("MessageForwardingInCaseOfFault");
    }

    class Bootstrapper : INeedInitialization
    {
        public void Init()
        {
            if (!Configure.SendOnlyMode && !Configure.Instance.Configurer.HasComponent<IManageMessageFailures>())
                Configure.Instance.MessageForwardingInCaseOfFault();
        }
    }
}
