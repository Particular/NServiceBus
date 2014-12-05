namespace NServiceBus
{
    using System.Configuration;
    using Config;
    using Logging;
    using Utils;

    static class ErrorQueueSettings
    {
        public static Address GetConfiguredErrorQueue(this Configure config)
        {
            var errorQueue = Address.Undefined;

            var section = Configure.GetConfigSection<MessageForwardingInCaseOfFaultConfig>();
            if (section != null)
            {
                if (string.IsNullOrWhiteSpace(section.ErrorQueue))
                {
                    throw new ConfigurationErrorsException(
                        "'MessageForwardingInCaseOfFaultConfig' configuration section is found but 'ErrorQueue' value is missing." +
                        "\n The following is an example for adding such a value to your app config: " +
                        "\n <MessageForwardingInCaseOfFaultConfig ErrorQueue=\"error\"/> \n");
                }

                Logger.Debug("Error queue retrieved from <MessageForwardingInCaseOfFaultConfig> element in config file.");

                errorQueue = Address.Parse(section.ErrorQueue);

                return errorQueue;
            }


            var errorQueueString = RegistryReader<string>.Read("ErrorQueue");
            if (!string.IsNullOrWhiteSpace(errorQueueString))
            {
                Logger.Debug("Error queue retrieved from registry settings.");
                errorQueue = Address.Parse(errorQueueString);
            }

            if (errorQueue == Address.Undefined)
            {
                throw new ConfigurationErrorsException("Faults forwarding requires an error queue to be specified. Please add a 'MessageForwardingInCaseOfFaultConfig' section to your app.config" +
                                                       "\n or configure a global one using the powershell command: Set-NServiceBusLocalMachineSettings -ErrorQueue {address of error queue}");
            }

            return errorQueue;
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(ErrorQueueSettings));
    }

}