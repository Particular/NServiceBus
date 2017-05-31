namespace NServiceBus
{
    using System;
    using Settings;

    /// <summary>
    /// Utility class to find the configured audit queue for an endpoint.
    /// </summary>
    public static class AuditConfigReader
    {
        /// <summary>
        /// Finds the configured audit queue for an endpoint.
        /// The audit queue can be configured using 'EndpointConfiguration.AuditProcessedMessagesTo()'.
        /// </summary>
        /// <param name="settings">The configuration settings for the endpoint.</param>
        /// <param name="address">When the method returns, this parameter will contain the configured audit queue address for the endpoint.</param>
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

        /// <summary>
        /// Returns the requested audit message expiration time if one is configured.
        /// </summary>
        /// <param name="settings">The configuration settings for the endpoint.</param>
        /// <param name="auditMessageExpiration">When the method returns, this parameter will contain the configured expiration time for audit messages.</param>
        /// <returns>True if audit message expiration is configured, false otherwise.</returns>
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
