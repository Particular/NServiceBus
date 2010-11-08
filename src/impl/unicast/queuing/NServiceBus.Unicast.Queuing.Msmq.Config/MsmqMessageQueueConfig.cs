using System.Configuration;

namespace NServiceBus.Config
{
    /// <summary>
    /// Contains the properties representing the MsmqMessageQueue configuration section.
    /// </summary>
    public class MsmqMessageQueueConfig : ConfigurationSection
    {

        ///<summary>
        /// If true, then message-delivery failure should result in a copy of the message being sent to a dead-letter queue
        ///</summary>
        [ConfigurationProperty("UseDeadLetterQueue", IsRequired = false)]
        public bool UseDeadLetterQueue
        {
            get
            {
                return (bool)this["UseDeadLetterQueue"];
            }
            set
            {
                this["UseDeadLetterQueue"] = value;
            }
        }

        ///<summary>
        /// If true, require that a copy of a message be kept in the originating computer's machine journal after the message has been successfully transmitted (from the originating computer to the next server)
        ///</summary>
        [ConfigurationProperty("UseJournalQueue", IsRequired = false)]
        public bool UseJournalQueue
        {
            get
            {
                return (bool)this["UseJournalQueue"];
            }
            set
            {
                this["UseJournalQueue"] = value;
            }
        }

    }
}
