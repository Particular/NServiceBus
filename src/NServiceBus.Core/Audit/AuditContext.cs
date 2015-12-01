namespace NServiceBus.Audit
{
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    /// <summary>
    /// Provide context to behaviors on the audit pipeline.
    /// </summary>
    public interface AuditContext : BehaviorContext
    {
        /// <summary>
        /// The message to be audited.
        /// </summary>
        OutgoingMessage Message { get; }

        /// <summary>
        /// Address of the audit queue.
        /// </summary>
        string AuditAddress { get; }
    }
}