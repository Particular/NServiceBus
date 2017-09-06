namespace NServiceBus
{
    using System;
    using Logging;
    using Settings;

    /// <summary>
    /// Utility class used to find the configured error queue for an endpoint.
    /// </summary>
    public static class ErrorQueueSettings
    {
        /// <summary>
        /// Finds the configured error queue for an endpoint.
        /// The error queue can be configured in code using 'EndpointConfiguration.SendFailedMessagesTo()'.
        /// </summary>
        /// <param name="settings">The configuration settings of this endpoint.</param>
        /// <returns>The configured error queue of the endpoint.</returns>
        /// <exception cref="Exception">When the configuration for the endpoint is invalid.</exception>
        public static string ErrorQueueAddress(this ReadOnlySettings settings)
        {
            Guard.AgainstNull(nameof(settings), settings);

            if (TryGetExplicitlyConfiguredErrorQueueAddress(settings, out var errorQueue))
            {
                return errorQueue;
            }

            return DefaultErrorQueueName;
        }

        /// <summary>
        /// Gets the explicitly configured error queue address if one is defined.
        /// The error queue can be configured in code by using 'EndpointConfiguration.SendFailedMessagesTo()'.
        /// </summary>
        /// <param name="settings">The configuration settings of this endpoint.</param>
        /// <param name="errorQueue">The configured error queue.</param>
        /// <returns>True if an error queue has been explicitly configured.</returns>
        /// <exception cref="Exception">When the configuration for the endpoint is invalid.</exception>
        public static bool TryGetExplicitlyConfiguredErrorQueueAddress(this ReadOnlySettings settings, out string errorQueue)
        {
            Guard.AgainstNull(nameof(settings), settings);
            if (settings.HasExplicitValue(SettingsKey))
            {
                Logger.Debug("Error queue retrieved from code configuration via 'EndpointConfiguration.SendFailedMessagesTo()'.");
                errorQueue = settings.Get<string>(SettingsKey);
                return true;
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