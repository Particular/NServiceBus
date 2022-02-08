namespace NServiceBus
{
    using Pipeline;
    using Routing;
    using Transport;

    /// <summary>
    /// Contains extensions methods to map behavior contexts.
    /// </summary>
    public static class AuditContextExtensions
    {
        /// <summary>
        /// Creates a <see cref="IRoutingContext" /> based on the current context.
        /// </summary>
        public static IRoutingContext CreateRoutingContext(this IAuditActionContext context, OutgoingMessage auditMessage, RoutingStrategy routingStrategy)
        {
            Guard.AgainstNull(nameof(auditMessage), auditMessage);
            Guard.AgainstNull(nameof(context), context);
            Guard.AgainstNull(nameof(routingStrategy), routingStrategy);

            return new RoutingContext(auditMessage, routingStrategy, context);
        }
    }
}