namespace NServiceBus.Audit
{
    using System.Collections.Generic;
    using NServiceBus.Transport;
    using Pipeline;

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
        /// Address of the audit queue.
        /// </summary>
        string AuditAddress { get; }

        /// <summary>
        /// Metadata for this message.
        /// </summary>
        IReadOnlyDictionary<string, string> AuditMetadata { get; }
    }
}