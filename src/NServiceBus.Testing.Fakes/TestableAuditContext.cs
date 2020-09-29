namespace NServiceBus.Testing
{
    using System;
    using System.Collections.Generic;
    using Pipeline;
    using Transport;

    /// <summary>
    /// A testable implementation of <see cref="IAuditContext" />.
    /// </summary>
    public partial class TestableAuditContext : TestableBehaviorContext, IAuditContext
    {
        /// <summary>
        /// Contains the information added by <see cref="AddedAuditData" />.
        /// </summary>
        public IDictionary<string, string> AddedAuditData { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Address of the audit queue.
        /// </summary>
        public string AuditAddress { get; set; } = "audit queue address";

        /// <summary>
        /// The message to be audited.
        /// </summary>
        public OutgoingMessage Message { get; set; } = new OutgoingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), new byte[0]);

        /// <summary>
        /// Adds information about the current message that should be audited.
        /// </summary>
        /// <param name="key">The audit key.</param>
        /// <param name="value">The value.</param>
        public void AddAuditData(string key, string value)
        {
            AddedAuditData.Add(key, value);
        }
    }
}