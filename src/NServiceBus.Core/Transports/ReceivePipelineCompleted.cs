namespace NServiceBus
{
    using System;
    using Transport;

    /// <summary>
    /// The ReceivePipeline completed event.
    /// </summary>
    public class ReceivePipelineCompleted
    {
        readonly DateTime startedAt;

        /// <summary>
        /// IncomingMessage.
        /// </summary>
        public IncomingMessage ProcessedMessage { get; }

        /// <summary>
        /// The time the reciving pipline started.
        /// </summary>
        public DateTime StartedAt
        {
            get { return startedAt; }
        }

        /// <summary>
        /// The time the reciving pipline completed.
        /// </summary>
        public DateTime CompletedAt { get; }

        internal ReceivePipelineCompleted(IncomingMessage processedMessage, DateTime startedAt, DateTime completedAt)
        {
            ProcessedMessage = processedMessage;
            this.startedAt = startedAt;
            CompletedAt = completedAt;
        }
    }
}