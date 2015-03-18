namespace NServiceBus.Faults
{
    using System.Configuration;
    using NServiceBus.Config;
    using NServiceBus.Logging;
    using NServiceBus.Settings;
    using NServiceBus.Utils;

    static class ErrorQueueSettings
    {
        public static string GetConfiguredErrorQueue(ReadOnlySettings settings)
        {
            string errorQueue = null;

            var section = settings.GetConfigSection<MessageForwardingInCaseOfFaultConfig>();
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

                errorQueue = section.ErrorQueue;
            }
            else
            {
                var registryErrorQueue = RegistryReader.Read("ErrorQueue");
                if (!string.IsNullOrWhiteSpace(registryErrorQueue))
                {
                    Logger.Debug("Error queue retrieved from registry settings.");
                    errorQueue = registryErrorQueue;
                }
            }

            if (errorQueue == null)
            {
                throw new ConfigurationErrorsException("Faults forwarding requires an error queue to be specified. Please add a 'MessageForwardingInCaseOfFaultConfig' section to your app.config" +
                                                       "\n or configure a global one using the powershell command: Set-NServiceBusLocalMachineSettings -ErrorQueue {address of error queue}");
            }

            return errorQueue;

        }

        static ILog Logger = LogManager.GetLogger(typeof(ErrorQueueSettings));
    }
}