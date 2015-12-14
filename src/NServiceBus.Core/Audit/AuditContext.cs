namespace NServiceBus
{
    using NServiceBus.Audit;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    /// <summary>
    /// Provides context to behaviors on the audit pipeline.
    /// </summary>
    public class AuditContext : BehaviorContext, IAuditContext
    {
        /// <summary>
        /// Creates a new instance of the audit context.
        /// </summary>
        /// <param name="message">The message to be audited.</param>
        /// <param name="auditAddress">The audit queue address.</param>
        /// <param name="parent">The parent context.</param>
        public AuditContext(OutgoingMessage message, string auditAddress, IBehaviorContext parent)
            : base(parent)
        {
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNullAndEmpty(nameof(auditAddress), auditAddress);
            Message = message;
            AuditAddress = auditAddress;
        }

        /// <summary>
        /// The message to be audited.
        /// </summary>
        public OutgoingMessage Message { get; }

        /// <summary>
        /// Address of the audit queue.
        /// </summary>
        public string AuditAddress { get; }
    }
}