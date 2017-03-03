namespace NServiceBus
{
    using System;
    using Transport;

    /// <summary>
    /// Event raised when a receive pipeline is completed.
    /// </summary>
    public class ReceivePipelineCompleted
    {
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

        /// <summary>
        /// The processed message.
        /// </summary>
        public IncomingMessage ProcessedMessage { get; }

        /// <summary>
        /// Time when the receive pipeline started.
        /// </summary>
        public DateTime StartedAt { get; }

        /// <summary>
        /// Time when the receive pipeline completed.
        /// </summary>
        public DateTime CompletedAt { get; }
    }
}