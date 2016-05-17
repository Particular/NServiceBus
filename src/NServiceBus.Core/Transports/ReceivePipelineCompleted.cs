namespace NServiceBus
{
    using System;
    using Transports;

    class ReceivePipelineCompleted
    {
        public IncomingMessage ProcessedMessage { get; }
        public DateTime StartedAt { get; }
        public DateTime CompletedAt { get; }

        public ReceivePipelineCompleted(IncomingMessage processedMessage, DateTime startedAt, DateTime completedAt)
        {
            ProcessedMessage = processedMessage;
            StartedAt = startedAt;
            CompletedAt = completedAt;
        }
    }
}