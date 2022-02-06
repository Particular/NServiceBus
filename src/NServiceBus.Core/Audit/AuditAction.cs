namespace NServiceBus.Audit
{
    using Transport;
    using System.Collections.Generic;
    using Pipeline;
    using Routing;

    /// <summary>
    /// Base class for audit actions.
    /// </summary>
    public abstract class AuditAction
    {
        /// <summary>
        /// Gets the messages, if any, this audit operation should result in.
        /// </summary>
        public abstract IEnumerable<(OutgoingMessage, RoutingStrategy)> GetRoutingData(IAuditContext context);
    }
}