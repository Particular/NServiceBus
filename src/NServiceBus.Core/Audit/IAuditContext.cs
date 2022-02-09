namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using NServiceBus.Audit;
    using Transport;

    /// <summary>
    /// Provide context to behaviors on the audit pipeline.
    /// </summary>
    public interface IAuditContext : IBehaviorContext
    {
        /// <summary>
        /// The message to be audited.
        /// </summary>
        OutgoingMessage Message { get; }

        /// <summary>
        /// Address of the audit queue.
        /// </summary>
        string AuditAddress { get; }

        /// <summary>
        /// The configured time to be received for audit messages.
        /// </summary>
        TimeSpan? TimeToBeReceived { get; }

        /// <summary>
        /// Adds information about the current message that should be audited.
        /// </summary>
        /// <param name="key">The audit key.</param>
        /// <param name="value">The value.</param>
        [ObsoleteEx(
            ReplacementTypeOrMember = nameof(AuditMetadata),
            TreatAsErrorFromVersion = "9.0",
            RemoveInVersion = "10.0")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        void AddAuditData(string key, string value);

        /// <summary>
        /// Metadata for the audited message.
        /// </summary>
        Dictionary<string, string> AuditMetadata { get; }

        /// <summary>
        /// The action to take for this audit message.
        /// </summary>
        AuditAction AuditAction { get; set; }

        /// <summary>
        /// Locks the context for further changes.
        /// </summary>
        IAuditActionContext PreventChanges();
    }
}