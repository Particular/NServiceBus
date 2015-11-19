namespace NServiceBus.Audit
{
    using Transports;
    using Pipeline;

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

    /// <summary>
    /// Provide context to behaviors on the audit pipeline.
    /// </summary>
    class AuditContextImpl : BehaviorContextImpl, AuditContext
    {
        /// <summary>
        /// The message to be audited.
        /// </summary>
        public OutgoingMessage Message { get; private set; }

        /// <summary>
        /// Address of the audit queue.
        /// </summary>
        public string AuditAddress { get; private set; }

        /// <summary>
        /// Initializes a new instance of <see cref="AuditContext"/>.
        /// </summary>
        /// <param name="message">The message to be audited.</param>
        /// <param name="auditAddress">The address of the audit queue to use.</param>
        /// <param name="parent">The parent incoming context.</param>
        public AuditContextImpl(OutgoingMessage message,string auditAddress, BehaviorContext parent)
            : base(parent)
        {
            Message = message;
            AuditAddress = auditAddress;
        }
    }
}