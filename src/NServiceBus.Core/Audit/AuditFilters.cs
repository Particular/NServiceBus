using System;

namespace NServiceBus.Audit
{
    using System.Collections.Generic;

    public class AuditFilters
    {
        public AuditFilters()
        {
            this.Filters = new HashSet<Func<TransportMessage, bool>>();
        }

        public ICollection<Func<TransportMessage, bool>> Filters { get; private set; }

        public AuditFilters ExcludeFromAudit(Func<TransportMessage, bool> excludeWhenTrueFunc)
        {
            this.Filters.Add(excludeWhenTrueFunc);

            return this;
        }
    }
}
