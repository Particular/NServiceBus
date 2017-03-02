namespace NServiceBus
{
    using System;
    using Transport;

    /// <summary>
    /// The ReceivePipeline completed event.
    /// </summary>
    public class ReceivePipelineCompleted
    {
        /// <summary>
        /// The message that was processed.
        /// </summary>
        public IncomingMessage ProcessedMessage { get; }

        /// <summary>
        /// Time when the receive pipline started.
        /// </summary>
        public DateTime StartedAt { get; }

        /// <summary>
        /// Time when the receive pipline completed.
        /// </summary>
        public DateTime CompletedAt { get; }

        /// <summary>
        /// Constructs the event.
        /// </summary>
        public ReceivePipelineCompleted(IncomingMessage processedMessage, DateTime startedAt, DateTime completedAt)
        {
            Guard.AgainstNull(nameof(processedMessage), processedMessage);
            Guard.AgainstNull(nameof(startedAt), startedAt);
            Guard.AgainstNull(nameof(completedAt), completedAt);

            ProcessedMessage = processedMessage;
            StartedAt = startedAt;
            CompletedAt = completedAt;
        }
    }
}