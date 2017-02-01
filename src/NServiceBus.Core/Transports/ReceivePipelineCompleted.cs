namespace NServiceBus
{
    using System;
    using Transport;

    /// <summary>
    /// 
    /// </summary>
    public class ReceivePipelineCompleted
    {
        /// <summary>
        /// 
        /// </summary>
        public IncomingMessage ProcessedMessage { get; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime StartedAt { get; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime CompletedAt { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="processedMessage"></param>
        /// <param name="startedAt"></param>
        /// <param name="completedAt"></param>
        public ReceivePipelineCompleted(IncomingMessage processedMessage, DateTime startedAt, DateTime completedAt)
        {
            ProcessedMessage = processedMessage;
            StartedAt = startedAt;
            CompletedAt = completedAt;
        }
    }
}