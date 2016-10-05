namespace NServiceBus
{
    using System;

    /// <summary>
    /// Contains extension methods to <see cref="EndpointConfiguration" />.
    /// </summary>
    public static class ConfigureAudit
    {
        /// <summary>
        /// Configure Audit settings.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        /// <param name="auditQueue">The name of the audit queue to use.</param>
        /// <param name="timeToBeReceived">The custom TTR to use for messages sent to the audit queue.</param>
        public static void AuditProcessedMessagesTo(this EndpointConfiguration config, string auditQueue, TimeSpan? timeToBeReceived = null)
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNullAndEmpty(nameof(auditQueue), auditQueue);
            if (timeToBeReceived != null)
            {
                Guard.AgainstNegative(nameof(timeToBeReceived), timeToBeReceived.Value);
            }
            config.Settings.Set<AuditConfigReader.Result>(new AuditConfigReader.Result
            {
                Address = auditQueue,
                TimeToBeReceived = timeToBeReceived
            });
        }
    }
}