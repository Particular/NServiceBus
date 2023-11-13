namespace NServiceBus;

using System;
using System.Collections.Generic;
using NServiceBus.Audit;
using Pipeline;
using Transport;

class AuditContext : BehaviorContext, IAuditContext, IAuditActionContext
{
    public AuditContext(OutgoingMessage message, string auditAddress, TimeSpan? timeToBeReceived, IBehaviorContext parent)
        : base(parent)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(auditAddress);
        Message = message;
        AuditAddress = auditAddress;
        TimeToBeReceived = timeToBeReceived;
        AuditMetadata = [];
        AuditAction = RouteToAudit.Instance;
    }

    public OutgoingMessage Message { get; }

    public string AuditAddress { get; }

    public TimeSpan? TimeToBeReceived { get; }

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

    public IAuditActionContext PreventChanges()
    {
        locked = true;
        return this;
    }

    AuditAction auditAction;
    bool locked;
}