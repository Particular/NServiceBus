namespace NServiceBus.Audit
{
    using System;

    public static class IAuditFiltersExtensions
    {
        public static IAuditFilters ExcludeMessageTypeFromAudit(this IAuditFilters auditFilters, Type typeToExclude)
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