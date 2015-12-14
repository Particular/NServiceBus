namespace NServiceBus
{
    using NServiceBus.Audit;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    class AuditContextImpl : BehaviorContextImpl, AuditContext
    {
        public AuditContextImpl(OutgoingMessage message, string auditAddress, BehaviorContext parent)
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