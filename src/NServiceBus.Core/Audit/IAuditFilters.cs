namespace NServiceBus.Audit
{
    using System;

    public interface IAuditFilters
    {
        IAuditFilters ExcludeFromAudit(Func<TransportMessage, bool> excludeWhenTrueFunc);
    }
}