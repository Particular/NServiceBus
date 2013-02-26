namespace NServiceBus.Config
{
    using System.Configuration;
    using System.Data.Common;
    using Transports.Msmq.Config;

    /// <summary>
    /// Contains the properties representing the MsmqMessageQueue configuration section.
    /// </summary>
    [ObsoleteEx(Message = "Use NServiceBus/Transport connectionString instead.", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]
    public class MsmqMessageQueueConfig : ConfigurationSection
    {

        ///<summary>
        /// If true, then message-delivery failure should result in a copy of the message being sent to a dead-letter queue
        ///</summary>
        [ConfigurationProperty("UseDeadLetterQueue", IsRequired = false, DefaultValue = true)]
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

    class MsmqConnectionStringBuilder : DbConnectionStringBuilder
    {
        public MsmqConnectionStringBuilder(string connectionString)
        {
            
            ConnectionString = connectionString;
        }

        public MsmqSettings RetrieveSettings()
        {
            var settings = new MsmqSettings();

            if (ContainsKey("deadLetter"))
                settings.UseDeadLetterQueue = bool.Parse((string)this["deadLetter"]);

            if (ContainsKey("journal"))
                settings.UseJournalQueue = bool.Parse((string) this["journal"]);

            if (ContainsKey("cacheSendConnection"))
                settings.UseConnectionCache = bool.Parse((string)this["cacheSendConnection"]);

            if (ContainsKey("useTransactionalQueues"))
                settings.UseTransactionalQueues = bool.Parse((string)this["useTransactionalQueues"]);

           
            return settings;
        }
    }
}
