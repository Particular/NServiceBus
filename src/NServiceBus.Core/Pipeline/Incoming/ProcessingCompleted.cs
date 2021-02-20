namespace NServiceBus
{
    using System;

    /// <summary>
    /// Event raised when the transport has completed processing a message.
    /// </summary>
    public class ProcessingCompleted
    {
        /// <summary>
        /// Constructs the event.
        /// </summary>
        public ProcessingCompleted(string messageId, bool wasAcknowledged, DateTimeOffset startedAt, DateTimeOffset completedAt)
        {
            MessageId = messageId;
            WasAcknowledged = wasAcknowledged;
            StartedAt = startedAt;
            CompletedAt = completedAt;
        }

        /// <summary>
        /// The ID of the message.
        /// </summary>
        public string MessageId;

        /// <summary>
        /// True if the message was acknowledged and removed from the queue.
        /// </summary>
        public bool WasAcknowledged;


        /// <summary>
        /// The time that processing started.
        /// </summary>
        public DateTimeOffset StartedAt { get; }

        /// <summary>
        /// The time processing completed.
        /// </summary>
        public DateTimeOffset CompletedAt { get; }
    }
}