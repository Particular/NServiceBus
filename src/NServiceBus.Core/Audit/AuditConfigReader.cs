namespace NServiceBus
{
    using System;
    using Settings;

    /// <summary>
    /// A utility class to get the configured audit queue settings.
    /// </summary>
    public static class AuditConfigReader
    {
        /// <summary>
        /// Gets the audit queue address for the endpoint.
        /// The audit queue address can be configured using 'EndpointConfiguration.AuditProcessedMessagesTo()'.
        /// </summary>
        /// <param name="settings">The configuration settings for the endpoint.</param>
        /// <param name="address">When this method returns, contains the audit queue address for the endpoint, if it has been configured, or null if it has not.</param>
        /// <returns>True if an audit queue address is configured; otherwise, false.</returns>
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

        /// <summary>
        /// Gets the audit message expiration time span for the endpoint.
        /// The audit message expiration time span can be configured using 'EndpointConfiguration.AuditProcessedMessagesTo()'.
        /// </summary>
        /// <param name="settings">The configuration settings for the endpoint.</param>
        /// <param name="auditMessageExpiration">When this method returns, contains the audit message expiration time span, if it has been configured, or TimeSpan.Zero if has not.</param>
        /// <returns>True if an audit message expiration time span is configured; otherwise, false.</returns>
        public static bool TryGetAuditMessageExpiration(this ReadOnlySettings settings, out TimeSpan auditMessageExpiration)
        {
            Guard.AgainstNull(nameof(settings), settings);

            var result = GetConfiguredAuditQueue(settings);

            if (result?.TimeToBeReceived == null)
            {
                auditMessageExpiration = TimeSpan.Zero;
                return false;
            }

            auditMessageExpiration = result.TimeToBeReceived.Value;
            return true;
        }

        internal static Result GetConfiguredAuditQueue(ReadOnlySettings settings)
        {
            return settings.TryGet(out Result configResult) ? configResult : null;
        }

        internal class Result
        {
            public string Address;
            public TimeSpan? TimeToBeReceived;
        }
    }
}
