namespace NServiceBus.Routing
{
    using NServiceBus.TransportDispatch;

    /// <summary>
    /// Provides ways to manipulate routing via the various contexts
    /// </summary>
    public static class RoutingContextExtensions
    {
        /// <summary>
        /// Gets the current routing strategy
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static RoutingStrategy GetRoutingStrategy(this DispatchContext context)
        {
            Guard.AgainstNull(context, "context");

            return context.Get<RoutingStrategy>();
        }

      
    }
}