namespace NServiceBus
{
    using System;

    /// <summary>
    /// Contains extension methods to <see cref="BusConfiguration"/>.
    /// </summary>
    public static class ConfigureAudit
    {

        /// <summary>
        /// Configure Audit settings. 
        /// </summary>
        public static void Audit(this BusConfiguration config, string auditQueue, TimeSpan? timeToBeReceived = null)
        {
            Guard.AgainstNull(config, "config");
            Guard.AgainstNullAndEmpty(auditQueue, "auditQueue");

            config.Settings.Set<AuditConfigReader.Result>(new AuditConfigReader.Result
            {
                Address = auditQueue,
                TimeToBeReceived = timeToBeReceived
            });
        }
    }
}