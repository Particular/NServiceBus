namespace NServiceBus.Testing
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Audit;
    using Pipeline;
    using Transport;

    /// <summary>
    /// A testable implementation of <see cref="IAuditContext" />.
    /// </summary>
    public partial class TestableAuditContext : TestableBehaviorContext, IAuditContext, IAuditActionContext
    {
        /// <summary>
        /// Address of the audit queue.
        /// </summary>
        public string AuditAddress { get; set; } = "audit queue address";

        /// <summary>
        /// The configured time to be received for audit messages.
        /// </summary>
        public TimeSpan? TimeToBeReceived { get; } = null;

        /// <summary>
        /// The message to be audited.
        /// </summary>
        public OutgoingMessage Message { get; set; } = new OutgoingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), new byte[0]);

        /// <summary>
        /// Metadata of the audited message.
        /// </summary>
        public Dictionary<string, string> AuditMetadata { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets the messages, if any, this audit operation should result in.
        /// </summary>
        public AuditAction AuditAction { get; set; }

        IReadOnlyDictionary<string, string> IAuditActionContext.AuditMetadata => AuditMetadata;

        /// <summary>
        /// Adds information about the current message that should be audited.
        /// </summary>
        /// <param name="key">The audit key.</param>
        /// <param name="value">The value.</param>
        public void AddAuditData(string key, string value)
        {
            AuditMetadata.Add(key, value);
        }

        public IAuditActionContext PreventChanges()
        {
            return this;
        }
    }
}