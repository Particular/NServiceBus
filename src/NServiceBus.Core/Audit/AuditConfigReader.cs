namespace NServiceBus
{
    using System;
    using Logging;
    using Settings;

    /// <summary>
    /// Utility class to find the configured audit queue for an endpoint.
    /// </summary>
    public static class AuditConfigReader
    {
        /// <summary>
        /// Finds the configured audit queue for an endpoint.
        /// The audit queue can be configured by using 'EndpointConfiguration.AuditProcessedMessagesTo()'
        /// or by using the 'HKEY_LOCAL_MACHINE\SOFTWARE\ParticularSoftware\ServiceBus\AuditQueue' registry key.
        /// </summary>
        /// <param name="settings">The configuration settings for the endpoint.</param>
        /// <param name="address">The configured audit queue address for the endpoint.</param>
        /// <returns>True if a configured audit address can be found, false otherwise.</returns>
        public static bool TryGetAuditQueueAddress(this ReadOnlySettings settings, out string address)
        {
            Guard.AgainstNull(nameof(settings), settings);

            var result = GetConfiguredAuditQueue(settings);
            if (result == null)
            {
                address = null;
                return false;
            }

            address = result.Address;
            return true;
        }

        internal static Result GetConfiguredAuditQueue(ReadOnlySettings settings)
        {
            return settings.TryGet(out Result configResult) ? configResult : null;
        }

        static ILog Logger = LogManager.GetLogger(typeof(AuditConfigReader));

        internal class Result
        {
            public string Address;
            public TimeSpan? TimeToBeReceived;
        }
    }
}
