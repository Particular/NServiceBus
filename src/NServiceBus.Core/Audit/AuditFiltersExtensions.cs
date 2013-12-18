namespace NServiceBus.Audit
{
    using System;

    public static class AuditFiltersExtensions
    {
        public static AuditFilters ExcludeMessageTypeFromAudit(this AuditFilters auditFilters, Type typeToExclude)
        {
            auditFilters.ExcludeFromAudit(message =>
            {
                var enclosedMessageTypes = message.Headers[Headers.EnclosedMessageTypes];

                return enclosedMessageTypes.Contains(typeToExclude.FullName);
            });

            return auditFilters;
        }
    }
}