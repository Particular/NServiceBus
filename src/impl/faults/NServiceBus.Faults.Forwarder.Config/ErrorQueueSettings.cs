using System.Configuration;
using Common.Logging;
using NServiceBus.Config;

namespace NServiceBus
{

    public static class ErrorQueueSettings
    {
        public static Address GetConfiguredErrorQueue(this Configure config)
        {
            var section = Configure.GetConfigSection<MessageForwardingInCaseOfFaultConfig>();
            if (section != null)
            {
                if (string.IsNullOrWhiteSpace(section.ErrorQueue))
                    throw new ConfigurationErrorsException(
                        "'MessageForwardingInCaseOfFaultConfig' configuration section is found but 'ErrorQueue' value is missing." +
                        "\n The following is an example for adding such a value to your app config: " +
                        "\n <MessageForwardingInCaseOfFaultConfig ErrorQueue=\"error\"/> \n");

                return Address.Parse(section.ErrorQueue);
            }
            Logger.Warn("Could not find configuration section 'MessageForwardingInCaseOfFaultConfig'. Going to try to find the error queue defined in 'MsmqTransportConfig'.");

            var msmq = Configure.GetConfigSection<MsmqTransportConfig>();

            if ((msmq == null) || (string.IsNullOrWhiteSpace(msmq.ErrorQueue)))
                throw new ConfigurationErrorsException("'MessageForwardingInCaseOfFaultConfig' configuration section is missing and could not find backup configuration section 'MsmqTransportConfig' in order to locate the error queue.");

            return Address.Parse(msmq.ErrorQueue);

        }

        static ILog Logger = LogManager.GetLogger("ErrorQueueSettings");
    }

}