using System;
using System.Configuration;
using NServiceBus.Config;
using NServiceBus.Faults;
using NServiceBus.Faults.Forwarder;
using NServiceBus.ObjectBuilder;
using NServiceBus.Logging;
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
            if (Endpoint.IsSendOnly)
                return config;
            var section = Configure.GetConfigSection<MessageForwardingInCaseOfFaultConfig>();
            if (section != null)
            {
                if (string.IsNullOrWhiteSpace(section.ErrorQueue))
                    throw new ConfigurationErrorsException(
                        "'MessageForwardingInCaseOfFaultConfig' configuration section is found but 'ErrorQueue' value is missing." +
                        "\n The following is an example for adding such a value to your app config: " +
                        "\n <MessageForwardingInCaseOfFaultConfig ErrorQueue=\"error\"/> \n");
                
                ErrorQueue = Address.Parse(section.ErrorQueue);
            }
            else
            {
                Logger.Warn("Could not find configuration section 'MessageForwardingInCaseOfFaultConfig'. Going to try to find the error queue defined in 'MsmqTransportConfig'.");

                var msmq = Configure.GetConfigSection<MsmqTransportConfig>();

                if ((msmq == null) || (string.IsNullOrWhiteSpace(msmq.ErrorQueue)))
                    throw new ConfigurationErrorsException("'MessageForwardingInCaseOfFaultConfig' configuration section is missing and could not find backup configuration section 'MsmqTransportConfig' in order to locate the error queue.");

                ErrorQueue = Address.Parse(msmq.ErrorQueue);
            }

            if(ErrorQueue == Address.Undefined)
				throw new ConfigurationErrorsException("Faults forwarding requires an error queue to be specified. Please add a 'MessageForwardingInCaseOfFaultConfig' section to your app.config");
             
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
            if (!Endpoint.IsSendOnly && !Configure.Instance.Configurer.HasComponent<IManageMessageFailures>())
                Configure.Instance.MessageForwardingInCaseOfFault();
        }
    }
}
