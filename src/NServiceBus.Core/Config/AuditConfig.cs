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
        [ConfigurationProperty("ForwardReceivedMessagesTo", IsRequired = false)]
        public string ForwardReceivedMessagesTo
        {
            get
            {
                var result = this["ForwardReceivedMessagesTo"] as string;
                if (string.IsNullOrWhiteSpace(result))
                    result = null;

                return result;
            }
            set
            {
                this["ForwardReceivedMessagesTo"] = value;
            }
        }

        /// <summary>
        /// Gets/sets the time to be received set on forwarded messages
        /// </summary>
        [ConfigurationProperty("TimeToBeReceivedOnForwardedMessages", IsRequired = false)]
        public TimeSpan TimeToBeReceivedOnForwardedMessages
        {
            get
            {
                return (TimeSpan)this["TimeToBeReceivedOnForwardedMessages"];
            }
            set
            {
                this["TimeToBeReceivedOnForwardedMessages"] = value;
            }
        }
    }
}
