namespace NServiceBus
{
    using System.Collections.Generic;
    using NServiceBus.Audit;
    using Pipeline;
    using Transport;

    class AuditContext : BehaviorContext, IAuditContext
    {
        public AuditContext(OutgoingMessage message, string auditAddress, IBehaviorContext parent)
            : base(parent)
        {
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNullAndEmpty(nameof(auditAddress), auditAddress);
            Message = message;
            AuditAddress = auditAddress;
            AuditMetadata = new Dictionary<string, string>();
        }

        public OutgoingMessage Message { get; }

        public string AuditAddress { get; }

        public IDictionary<string, string> AuditMetadata { get; }

        public AuditAction AuditAction { get; set; } = new SendToAudit();

        public void AddAuditData(string key, string value)
        {
            Guard.AgainstNullAndEmpty(nameof(key), key);
            Guard.AgainstNullAndEmpty(nameof(value), value);

            AuditMetadata[key] = value;
        }

    }
}