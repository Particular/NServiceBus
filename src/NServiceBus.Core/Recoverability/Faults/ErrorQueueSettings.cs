namespace NServiceBus
{
    using System;
    using Config;
    using Logging;
    using Settings;

    /// <summary>
    /// Utility class used to find the configured error queue for an endpoint.
    /// </summary>
    public static class ErrorQueueSettings
    {
        /// <summary>
        /// Finds the configured error queue for an endpoint.
        /// The error queue can be configured in code using 'EndpointConfiguration.SendFailedMessagesTo()',
        /// via the 'Error' attribute of the 'MessageForwardingInCaseOfFaultConfig' configuration section,
        /// or using the 'HKEY_LOCAL_MACHINE\SOFTWARE\ParticularSoftware\ServiceBus\ErrorQueue' registry key.
        /// </summary>
        /// <param name="settings">The configuration settings of this endpoint.</param>
        /// <returns>The configured error queue of the endpoint.</returns>
        /// <exception cref="Exception">When the configuration for the endpoint is invalid.</exception>
        public static string ErrorQueueAddress(this ReadOnlySettings settings)
        {
            string errorQueue;

            if (settings.TryGet("errorQueue", out errorQueue))
            {
                Logger.Debug("Error queue retrieved from code configuration via 'EndpointConfiguration.SendFailedMessagesTo().");
                return errorQueue;
            }

            var section = settings.GetConfigSection<MessageForwardingInCaseOfFaultConfig>();
            if (section != null)
            {
                if (!string.IsNullOrWhiteSpace(section.ErrorQueue))
                {
                    Logger.Debug("Error queue retrieved from <MessageForwardingInCaseOfFaultConfig> element in config file.");
                    return section.ErrorQueue;
                }

                throw new Exception(
                    @"'MessageForwardingInCaseOfFaultConfig' configuration section is found but 'ErrorQueue' value is empty.
Take one of the following actions:
- set the error queue at configuration time using 'EndpointConfiguration.SendFailedMessagesTo()'
- Add a valid value to to the app.config. For example:
 <MessageForwardingInCaseOfFaultConfig ErrorQueue=""error""/>");
            }

            var registryErrorQueue = RegistryReader.Read("ErrorQueue");
            if (registryErrorQueue != null)
            {
                if (!string.IsNullOrWhiteSpace(registryErrorQueue))
                {
                    Logger.Debug("Error queue retrieved from registry settings.");
                    return registryErrorQueue;
                }
                throw new Exception(
                    @"'ErrorQueue' read from registry but the value is empty.
Take one of the following actions:
- set the error queue at configuration time using 'EndpointConfiguration.SendFailedMessagesTo()'
- add a 'MessageForwardingInCaseOfFaultConfig' section to the app.config
- give 'HKEY_LOCAL_MACHINE\SOFTWARE\ParticularSoftware\ServiceBus\ErrorQueue' a valid value for the error queue");
            }

            throw new Exception(
                @"Faults forwarding requires an error queue to be specified.
Take one of the following actions:
- set the error queue at configuration time using 'EndpointConfiguration.SendFailedMessagesTo()'
- add a 'MessageForwardingInCaseOfFaultConfig' section to the app.config
- configure a global error queue in the registry using the powershell command: Set-NServiceBusLocalMachineSettings -ErrorQueue {address of error queue}");
        }

        static ILog Logger = LogManager.GetLogger(typeof(ErrorQueueSettings));
    }
}
