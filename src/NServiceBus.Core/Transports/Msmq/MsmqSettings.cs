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
            MessageEnumeratorTimeout = TimeSpan.FromSeconds(1); //with a 1s timeout a graceful shutdown will take on average 500ms which is acceptable
        }

        public bool UseDeadLetterQueue { get; set; }

        public bool UseJournalQueue { get; set; }

        public bool UseConnectionCache { get; set; }

        public bool UseTransactionalQueues { get; set; }

        public TimeSpan TimeToReachQueue { get; set; }

        public bool UseDeadLetterQueueForMessagesWithTimeToBeReceived { get; set; }

        public TimeSpan MessageEnumeratorTimeout { get; set; }
    }
}