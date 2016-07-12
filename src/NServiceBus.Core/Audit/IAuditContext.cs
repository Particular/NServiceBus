namespace NServiceBus.Pipeline
{
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
        /// Adds information about the current message that should be audited.
        /// </summary>
        /// <param name="key">The audit key.</param>
        /// <param name="value">The value.</param>
        void AddAuditData(string key, string value);
    }
}