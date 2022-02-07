namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Audit;
    using Pipeline;
    using Transport;

    class AuditContext : BehaviorContext, IAuditContext, IAuditActionContext
    {
        public AuditContext(OutgoingMessage message, string auditAddress, IBehaviorContext parent)
            : base(parent)
        {
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNullAndEmpty(nameof(auditAddress), auditAddress);
            Message = message;
            AuditAddress = auditAddress;
            AuditMetadata = new Dictionary<string, string>();
            AuditAction = RouteToAudit.Instance;
        }

        public OutgoingMessage Message { get; }

        public string AuditAddress { get; }

        public Dictionary<string, string> AuditMetadata { get; }

        IReadOnlyDictionary<string, string> IAuditActionContext.AuditMetadata => AuditMetadata;

        public AuditAction AuditAction
        {
            get => auditAction;
            set
            {
                if (locked)
                {
                    throw new InvalidOperationException("The AuditAction has already been executed and can't be changed");
                }
                auditAction = value;
            }
        }
        public void AddAuditData(string key, string value)
        {
            Guard.AgainstNullAndEmpty(nameof(key), key);
            Guard.AgainstNullAndEmpty(nameof(value), value);

            AuditMetadata[key] = value;
        }

        public IAuditActionContext PreventChanges()
        {
            locked = true;
            return this;
        }

        AuditAction auditAction;
        bool locked;
    }
}