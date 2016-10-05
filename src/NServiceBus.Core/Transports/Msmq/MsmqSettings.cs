namespace NServiceBus
{
    using System;
    using System.Messaging;

    class MsmqSettings
    {
        public MsmqSettings()
        {
            UseDeadLetterQueue = true;
            UseConnectionCache = true;
            UseTransactionalQueues = true;
            TimeToReachQueue = Message.InfiniteTimeout;
        }

        public bool UseDeadLetterQueue { get; set; }

        public bool UseJournalQueue { get; set; }

        public bool UseConnectionCache { get; set; }

        public bool UseTransactionalQueues { get; set; }

        public TimeSpan TimeToReachQueue { get; set; }
    }
}