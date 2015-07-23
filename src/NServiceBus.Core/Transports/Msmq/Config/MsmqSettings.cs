namespace NServiceBus.Transports.Msmq.Config
{
    using System;
    using System.Messaging;

    /// <summary>
    /// Runtime settings for the Msmq transport.
    /// </summary>
    public class MsmqSettings
    {

        /// <summary>
        /// Initializes a new instance of <see cref="MsmqSettings"/>.
        /// </summary>
        public MsmqSettings()
        {
            UseDeadLetterQueue = true;
            UseConnectionCache = true;
            UseTransactionalQueues = true;
            TimeToReachQueue = Message.InfiniteTimeout;
        }

        /// <summary>
        /// Determines if the dead letter queue should be used.
        /// </summary>
        public bool UseDeadLetterQueue { get; set; }

        /// <summary>
        /// Determines if journaling should be activated.
        /// </summary>
        public bool UseJournalQueue { get; set; }
        
        /// <summary>
        /// Gets or sets a value that indicates whether a cache of connections will be maintained by the application.
        /// </summary> 
        public bool UseConnectionCache { get; set; }

        /// <summary>
        /// Determines if the system uses transactional queues.
        /// </summary>
        public bool UseTransactionalQueues { get; set; }

        /// <summary>
        /// Gets or sets the maximum amount of time for the message to reach the queue.
        /// </summary>
        public TimeSpan TimeToReachQueue { get; set; }
    }
}