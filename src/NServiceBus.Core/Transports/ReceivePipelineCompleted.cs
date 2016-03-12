namespace NServiceBus
{
    using System;
    using Transports;

    class ReceivePipelineCompleted
    {
        public DateTime StartedAt { get; }
        public DateTime CompletedAt { get; }
        public IncomingMessage ProcessedMessage { get; }

        public ReceivePipelineCompleted(DateTime startedAt, DateTime completedAt, IncomingMessage processedMessage)
        {
            StartedAt = startedAt;
            CompletedAt = completedAt;
            ProcessedMessage = processedMessage;
        }
    }
}