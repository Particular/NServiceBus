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
    public partial class TestableAuditContext : TestableBehaviorContext, IAuditContext
    {
        /// <summary>
        /// Contains the information added by <see cref="AddAuditData" />.
        /// </summary>
        //TODO: obsolete with WARN
        public IDictionary<string, string> AddedAuditData => AuditMetadata;

        /// <summary>
        /// Address of the audit queue.
        /// </summary>
        public string AuditAddress { get; set; } = "audit queue address";

        /// <summary>
        /// The message to be audited.
        /// </summary>
        public OutgoingMessage Message { get; set; } = new OutgoingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), new byte[0]);

        /// <summary>
        /// Metadata of the audited message.
        /// </summary>
        public IDictionary<string, string> AuditMetadata { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets the message and routing strategies this audit operation should result in.
        /// </summary>
        public AuditAction AuditAction { get; set; } = new SendToAudit();

        /// <summary>
        /// Adds information about the current message that should be audited.
        /// </summary>
        /// <param name="key">The audit key.</param>
        /// <param name="value">The value.</param>
        [ObsoleteEx(
            ReplacementTypeOrMember = nameof(AuditMetadata),
            TreatAsErrorFromVersion = "9.0",
            RemoveInVersion = "10.0")]
        public void AddAuditData(string key, string value)
        {
            AuditMetadata.Add(key, value);
        }
    }
}