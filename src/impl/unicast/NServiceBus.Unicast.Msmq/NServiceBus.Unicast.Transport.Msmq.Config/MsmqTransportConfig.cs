using System.Configuration;

namespace NServiceBus.Config
{
    /// <summary>
    /// Contains the properties representing the MsmqTransport configuration section.
    /// </summary>
    public class MsmqTransportConfig : ConfigurationSection
    {
        /// <summary>
        /// The queue to receive messages from in the format
        /// "queue@machine".
        /// </summary>
        [ConfigurationProperty("InputQueue", IsRequired = true)]
        public string InputQueue
        {
            get
            {
                return this["InputQueue"] as string;
            }
            set
            {
                this["InputQueue"] = value;
            }
        }

        /// <summary>
        /// The queue to which to forward messages that could not be processed
        /// in the format "queue@machine".
        /// </summary>
        [ConfigurationProperty("ErrorQueue", IsRequired = true)]
        public string ErrorQueue
        {
            get
            {
                return this["ErrorQueue"] as string;
            }
            set
            {
                this["ErrorQueue"] = value;
            }
        }

        /// <summary>
        /// The number of worker threads that can process messages in parallel.
        /// </summary>
        [ConfigurationProperty("NumberOfWorkerThreads", IsRequired = true)]
        public int NumberOfWorkerThreads
        {
            get
            {
                return (int)this["NumberOfWorkerThreads"];
            }
            set
            {
                this["NumberOfWorkerThreads"] = value;
            }
        }

        /// <summary>
        /// The maximum number of times to retry processing a message
        /// when it fails before moving it to the error queue.
        /// </summary>
        [ConfigurationProperty("MaxRetries", IsRequired = true)]
        public int MaxRetries
        {
            get
            {
                return (int)this["MaxRetries"];
            }
            set
            {
                this["MaxRetries"] = value;
            }
        }

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
