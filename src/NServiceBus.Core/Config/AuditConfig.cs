namespace NServiceBus.Config
{
    using System;
    using System.Configuration;

    /// <summary>
    /// Config section for the auditing feature
    /// </summary>
    public class AuditConfig : ConfigurationSection
    {
        /// <summary>
        /// Gets/sets the address to which messages received will be forwarded.
        /// </summary>
        [ConfigurationProperty("QueueName", IsRequired = false)]
        public string QueueName
        {
            get
            {
                var result = this["QueueName"] as string;
                if (string.IsNullOrWhiteSpace(result))
                    result = null;

                return result;
            }
            set
            {
                this["QueueName"] = value;
            }
        }

        /// <summary>
        /// Gets/sets the time to be received set on forwarded messages
        /// </summary>
        [ConfigurationProperty("OverrideTimeToBeRecieved", IsRequired = false)]
        public TimeSpan OverrideTimeToBeRecieved
        {
            get
            {
                return (TimeSpan)this["OverrideTimeToBeRecieved"];
            }
            set
            {
                this["OverrideTimeToBeRecieved"] = value;
            }
        }
    }
}
