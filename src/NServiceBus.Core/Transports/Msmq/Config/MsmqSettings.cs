namespace NServiceBus.Transports.Msmq.Config
{
    /// <summary>
    /// Runtime settings for the Msmq transport
    /// </summary>
    public class MsmqSettings
    {

        /// <summary>
        /// Constructs the settings class with defaults
        /// </summary>
        public MsmqSettings()
        {
            UseDeadLetterQueue = true;
            UseConnectionCache = true;
            UseTransactionalQueues = true;
        }

        /// <summary>
        /// Determines if the dead letter queue should be used
        /// </summary>
        public bool UseDeadLetterQueue { get; set; }

        /// <summary>
        /// Determines if journaling should be activated
        /// </summary>
        public bool UseJournalQueue { get; set; }
        
        /// <summary>
        /// Gets or sets a value that indicates whether a cache of connections will be maintained by the application.
        /// </summary> 
        public bool UseConnectionCache { get; set; }

        /// <summary>
        /// Determines if the system uses transactional queues
        /// </summary>
        public bool UseTransactionalQueues { get; set; }
    }
}