namespace NServiceBus.Recoverability
{
    using Pipeline;
    using Routing;
    using Transport;

    /// <summary>
    /// Allows the dispatch pipeline to be invoked from the recoverability pipeline.
    /// </summary>
    public static class RecoverabilityContextExtensions
    {
        /// <summary>
        /// Creates a <see cref="IRoutingContext" /> based on the current context.
        /// </summary>
        public static IRoutingContext CreateRoutingContext(this IRecoverabilityActionContext context, OutgoingMessage outgoingMessage, RoutingStrategy routingStrategy) =>
            new RoutingContext(outgoingMessage, routingStrategy, context);
    }
}