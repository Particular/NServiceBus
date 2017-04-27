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

            if (TryGetExplicitlyConfiguredErrorQueueAddress(settings, out errorQueue))
                return errorQueue;

            return DefaultErrorQueueName;
        }

        /// <summary>
        /// Gets the explicitly configured error queue address if one is defined.
        /// The error queue can be configured in code using 'EndpointConfiguration.SendFailedMessagesTo()',
        /// via the 'Error' attribute of the 'MessageForwardingInCaseOfFaultConfig' configuration section,
        /// or using the 'HKEY_LOCAL_MACHINE\SOFTWARE\ParticularSoftware\ServiceBus\ErrorQueue' registry key.
        /// </summary>
        /// <param name="settings">The configuration settings of this endpoint.</param>
        /// <param name="errorQueue">The configured error queue.</param>
        /// <returns>True if an error queue has been explicitly configured.</returns>
        /// <exception cref="Exception">When the configuration for the endpoint is invalid.</exception>
        public static bool TryGetExplicitlyConfiguredErrorQueueAddress(this ReadOnlySettings settings, out string errorQueue)
        {
            if (settings.HasExplicitValue(SettingsKey))
            {
                Logger.Debug("Error queue retrieved from code configuration via 'EndpointConfiguration.SendFailedMessagesTo()'.");
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

                var message = "'MessageForwardingInCaseOfFaultConfig' configuration section exists, but 'ErrorQueue' value is empty. " +
                    $"Specify a value, or remove the configuration section so that the default error queue name, '{DefaultErrorQueueName}', will be used instead.";

                throw new Exception(message);
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

                var message = "'ErrorQueue' read from the registry, but the value is empty. Specify a value, or remove 'ErrorQueue' " +
                    $"from the registry so that the default error queue name, '{DefaultErrorQueueName}', will be used instead.";

                throw new Exception(message);
            }

            errorQueue = null;
            return false;
        }

        /// <summary>
        /// The settings key where the error queue address is stored.
        /// </summary>
        public const string SettingsKey = "errorQueue";

        const string DefaultErrorQueueName = "error";

        static ILog Logger = LogManager.GetLogger(typeof(ErrorQueueSettings));
    }
}