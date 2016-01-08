namespace NServiceBus
{
    using NServiceBus.Audit;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    class AuditContext : BehaviorContext, IAuditContext
    {
        public AuditContext(OutgoingMessage message, string auditAddress, IBehaviorContext parent)
            : base(parent)
        {
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNullAndEmpty(nameof(auditAddress), auditAddress);
            Message = message;
            AuditAddress = auditAddress;
        }

        public OutgoingMessage Message { get; }

        public string AuditAddress { get; }
    }
}