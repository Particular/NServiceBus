using System.Configuration;
using NServiceBus.Config;
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
            string errorQueue;

            var section = Configure.GetConfigSection<MessageForwardingInCaseOfFaultConfig>();
            if (section == null)
            {
                Logger.Warn("Could not find configuration section 'MessageForwardingInCaseOfFaultConfig'. Going to try to find the error queue defined in 'MsmqTransportConfig'.");

                var msmq = Configure.GetConfigSection<MsmqTransportConfig>();
                if (msmq == null)
                    throw new ConfigurationErrorsException("Could not find backup configuration section 'MsmqTransportConfig' in order to locate the error queue.");

                
                errorQueue = msmq.ErrorQueue;
            }
            else
                errorQueue = section.ErrorQueue;

			if(string.IsNullOrEmpty(errorQueue))
				throw new ConfigurationErrorsException("Faults forwarding requires a error queue to be specified. Please add a 'MessageForwardingInCaseOfFaultConfig' section to your app.config");
             
            //TODO: this should probably be moved to a new IManageFaults.Start|Init method instead. Check with Udi
            MsmqUtilities.CreateQueueIfNecessary(errorQueue);
	
            config.Configurer.ConfigureComponent<FaultManager>(ComponentCallModelEnum.Singlecall)
                .ConfigureProperty(fm => fm.ErrorQueue, errorQueue);

            return config;
        }

        private static ILog Logger = LogManager.GetLogger("MessageForwardingInCaseOfFault");
    }
}
