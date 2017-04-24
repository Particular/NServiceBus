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
        internal const string SettingsKey = "errorQueue";
        const string DefaultErrorQueueName = "error";

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

            if (TryGetErrorQueueAddress(settings, out errorQueue))
            {
                return errorQueue;
            }

            return DefaultErrorQueueName;
        }

        internal static bool TryGetErrorQueueAddress(this ReadOnlySettings settings, out string errorQueue)
        {
            if (settings.HasExplicitValue(SettingsKey))
            {
                Logger.Debug("Error queue retrieved from code configuration via 'EndpointConfiguration.SendFailedMessagesTo().");
                errorQueue = settings.Get<string>(SettingsKey);
                return true;
            }

            var section = settings.GetConfigSection<MessageForwardingInCaseOfFaultConfig>();
            if (section != null)
            {
                if (!string.IsNullOrWhiteSpace(section.ErrorQueue))
                {
                    Logger.Debug("Error queue retrieved from <MessageForwardingInCaseOfFaultConfig> element in config file.");
                    errorQueue = section.ErrorQueue;
                    return true;
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
                    errorQueue = registryErrorQueue;
                    return true;
                }
                throw new Exception(
                    @"'ErrorQueue' read from registry but the value is empty.
Take one of the following actions:
- set the error queue at configuration time using 'EndpointConfiguration.SendFailedMessagesTo()'
- add a 'MessageForwardingInCaseOfFaultConfig' section to the app.config
- give 'HKEY_LOCAL_MACHINE\SOFTWARE\ParticularSoftware\ServiceBus\ErrorQueue' a valid value for the error queue");
            }

            errorQueue = null;
            return false;
        }

        static ILog Logger = LogManager.GetLogger(typeof(ErrorQueueSettings));
    }
}
