namespace NServiceBus.Config
{
    using System;
    using System.Configuration;

    /// <summary>
    /// Config section for the auditing feature.
    /// </summary>
    [ObsoleteEx(
        Message = "Use of the application configuration file to configure audit is discouraged",
        RemoveInVersion = "7.0")]
    public class AuditConfig : ConfigurationSection
    {
        /// <summary>
        /// Gets/sets the address to which messages received will be forwarded.
        /// </summary>
        [ConfigurationProperty("QueueName", IsRequired = false)]
        [ObsoleteEx(
            Message = "Use of the application configuration file to configure audit is discouraged",
            ReplacementTypeOrMember = "EndpointConfiguration.AuditProcessedMessagesTo",
            RemoveInVersion = "7.0")]
        public string QueueName
        {
            get
            {
                var result = this["QueueName"] as string;
                if (string.IsNullOrWhiteSpace(result))
                {
                    return null;
                }

                return result;
            }
            set { this["QueueName"] = value; }
        }

        /// <summary>
        /// Gets/sets the time to be received set on forwarded messages.
        /// </summary>
        [ConfigurationProperty("OverrideTimeToBeReceived", IsRequired = false)]
        [ObsoleteEx(
            Message = "Use of the application configuration file to configure audit is discouraged",
            ReplacementTypeOrMember = "EndpointConfiguration.AuditProcessedMessagesTo",
            RemoveInVersion = "7.0")]
        public TimeSpan OverrideTimeToBeReceived
        {
            get { return (TimeSpan) this["OverrideTimeToBeReceived"]; }
            set
            {
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentException("OverrideTimeToBeReceived must be Zero (to indicate empty) or greater.", nameof(value));
                }
                this["OverrideTimeToBeReceived"] = value;
            }
        }
    }
}