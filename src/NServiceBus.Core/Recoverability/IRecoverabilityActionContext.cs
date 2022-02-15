namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Transport;

    /// <summary>
    /// Provide context to recoverability actions.
    /// </summary>
    public interface IRecoverabilityActionContext : IBehaviorContext
    {
        /// <summary>
        /// The message that failed processing.
        /// </summary>
        public IncomingMessage FailedMessage { get; }

        /// <summary>
        /// The exception that caused processing to fail.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// The receive address where this message failed.
        /// </summary>
        public string ReceiveAddress { get; }

        /// <summary>
        /// The number of times the message have been retried immediately but failed.
        /// </summary>
        public int ImmediateProcessingFailures { get; }

        /// <summary>
        /// Metadata for this message.
        /// </summary>
        IReadOnlyDictionary<string, string> Metadata { get; }
    }
}