#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics.CodeAnalysis;
using Logging;
using Settings;

/// <summary>
/// Utility class used to find the configured error queue for an endpoint.
/// </summary>
public static class ErrorQueueSettings
{
    /// <param name="settings">The configuration settings of this endpoint.</param>
    extension(IReadOnlySettings settings)
    {
        /// <summary>
        /// Finds the configured error queue for an endpoint.
        /// The error queue can be configured in code using 'EndpointConfiguration.SendFailedMessagesTo()'.
        /// </summary>
        /// <returns>The configured error queue of the endpoint.</returns>
        /// <exception cref="Exception">When the configuration for the endpoint is invalid.</exception>
        public string ErrorQueueAddress()
        {
            ArgumentNullException.ThrowIfNull(settings);

            return TryGetExplicitlyConfiguredErrorQueueAddress(settings, out var errorQueue) ? errorQueue : DefaultErrorQueueName;
        }

        /// <summary>
        /// Gets the explicitly configured error queue address if one is defined.
        /// The error queue can be configured in code by using 'EndpointConfiguration.SendFailedMessagesTo()'.
        /// </summary>
        /// <param name="errorQueue">The configured error queue.</param>
        /// <returns>True if an error queue has been explicitly configured.</returns>
        /// <exception cref="Exception">When the configuration for the endpoint is invalid.</exception>
        public bool TryGetExplicitlyConfiguredErrorQueueAddress([NotNullWhen(true)] out string? errorQueue)
        {
            ArgumentNullException.ThrowIfNull(settings);
            if (settings.HasExplicitValue(SettingsKey))
            {
                Logger.Debug("Error queue retrieved from code configuration via 'EndpointConfiguration.SendFailedMessagesTo()'.");
                errorQueue = settings.Get<string>(SettingsKey);
                return true;
            }

            errorQueue = null;
            return false;
        }
    }

    /// <summary>
    /// The settings key where the error queue address is stored.
    /// </summary>
    public const string SettingsKey = "errorQueue";

    const string DefaultErrorQueueName = "error";

    static readonly ILog Logger = LogManager.GetLogger(typeof(ErrorQueueSettings));
}