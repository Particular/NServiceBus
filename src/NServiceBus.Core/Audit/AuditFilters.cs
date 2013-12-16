using System;

namespace NServiceBus.Audit
{
    using System.Collections.Generic;
    using System.Linq;

    public class AuditFilters : IAuditFilters
    {
        private Lazy<ICollection<Func<TransportMessage, bool>>> filters;

        public AuditFilters()
        {
            this.filters = new Lazy<ICollection<Func<TransportMessage, bool>>>(() => new HashSet<Func<TransportMessage, bool>>());
        }

        public IAuditFilters ExcludeFromAudit(Func<TransportMessage, bool> excludeWhenTrueFunc)
        {
            this.filters.Value.Add(excludeWhenTrueFunc);

            return this;
        }

        public void ExcludeMessageTypeFromAudit(Type typeToExclude)
        {
            this.ExcludeFromAudit(message =>
            {
                var enclosedMessageTypes = message.Headers[Headers.EnclosedMessageTypes];

                return enclosedMessageTypes.Contains(typeToExclude.FullName);
            });
        }

        public bool AuditMessage(TransportMessage transportMessage)
        {
            if (!filters.IsValueCreated)
            {
                return true;
            }

            // do not filter the transport message if any of the filters requests an audit (filter returning false)
            return filters.Value.Any(f => !f(transportMessage));
        }
    }
}
