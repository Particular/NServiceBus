namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Transport;

    /// <summary>
    /// Provides context to audit actions.
    /// </summary>
    public interface IAuditActionContext : IBehaviorContext
    {
        /// <summary>
        /// Context for the message that failed processing.
        /// </summary>
        OutgoingMessage Message { get; }

        /// <summary>
        /// Metadata for this message.
        /// </summary>
        IReadOnlyDictionary<string, string> AuditMetadata { get; }

        /// <summary>
        /// Address of the audit queue.
        /// </summary>
        string AuditAddress { get; }

        /// <summary>
        /// The configured time to be received for audit messages.
        /// </summary>
        TimeSpan? TimeToBeReceived { get; }
    }
}