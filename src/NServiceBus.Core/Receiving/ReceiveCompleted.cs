namespace NServiceBus
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Event raised when the transport has completed processing a message.
    /// </summary>
    public class ReceiveCompleted
    {
        /// <summary>
        /// Constructs the event.
        /// </summary>
        public ReceiveCompleted(string messageId, bool wasAcknowledged, Dictionary<string, string> headers, DateTimeOffset startedAt, DateTimeOffset completedAt, bool processingFailed)
        {
            NativeMessageId = messageId;
            WasAcknowledged = wasAcknowledged;
            Headers = headers;
            StartedAt = startedAt;
            CompletedAt = completedAt;
            ProcessingFailed = processingFailed;
        }

        /// <summary>
        /// The native message ID.
        /// </summary>
        public string NativeMessageId { get; }

        /// <summary>
        /// True if the message was acknowledged and removed from the queue.
        /// </summary>
        public bool WasAcknowledged { get; }

        /// <summary>
        /// The message headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; }

        /// <summary>
        /// The time that processing started.
        /// </summary>
        public DateTimeOffset StartedAt { get; }

        /// <summary>
        /// The time processing completed.
        /// </summary>
        public DateTimeOffset CompletedAt { get; }

        /// <summary>
        /// True if the the message processing resulted in an exception.
        /// </summary>
        public bool ProcessingFailed { get; }
    }
}
