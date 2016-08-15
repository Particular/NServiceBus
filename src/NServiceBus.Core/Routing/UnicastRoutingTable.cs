namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;

    /// <summary>
    /// The unicast routing table.
    /// </summary>
    public class UnicastRoutingTable
    {
        internal Task<IUnicastRoute> GetRouteFor(Type messageType, ContextBag contextBag)
        {
            IUnicastRoute unicastRoute;
            if (staticRoutes.TryGetValue(messageType, out unicastRoute))
            {
                return Task.FromResult(unicastRoute);
            }

            if (fallbackRoute != null)
            {
                return fallbackRoute(messageType, contextBag);
            }

            return noRoute;
        }

        /// <summary>
        /// Adds a static unicast route for a given message type.
        /// </summary>
        /// <param name="messageType">The message type to use the route for.</param>
        /// <param name="route">The route to use for the given message type.</param>
        /// <exception cref="Exception">Throws an exception when an ambiguous route exists.</exception>
        public void RouteTo(Type messageType, IUnicastRoute route)
        {
            RouteTo(messageType, route, false);
        }

        /// <summary>
        /// Adds a static unicast route for a given message type.
        /// </summary>
        /// <param name="messageType">The message type to use the route for.</param>
        /// <param name="route">The route to use for the given message type.</param>
        /// <param name="overrideExistingRoute">
        /// Will override an existing route for the message type without throwing an exception
        /// when set to <code>true</code>.
        /// </param>
        /// <exception cref="Exception">
        /// Throws an exception when an ambiguous route exists and
        /// <paramref name="overrideExistingRoute" /> is not set to <code>true</code>.
        /// </exception>
        public void RouteTo(Type messageType, IUnicastRoute route, bool overrideExistingRoute)
        {
            if (!overrideExistingRoute && staticRoutes.ContainsKey(messageType))
            {
                throw new Exception($"The static routing table already contains a route for message '{messageType.Name}'. Remove the ambiguous route registrations or override the existing route.");
            }

            staticRoutes[messageType] = route;
        }

        /// <summary>
        /// Configures a dynamically executed fallback rule which is executed when no static route was found for a given message type.
        /// </summary>
        /// <param name="fallbackRoute">The dynamic rule which is invoked to determine the route for a given message type.</param>
        public void SetFallbackRoute(Func<Type, ContextBag, Task<IUnicastRoute>> fallbackRoute)
        {
            if (this.fallbackRoute != null)
            {
                throw new Exception("A custom fallback route has already been configured. Only one fallback route is supported.");
            }

            this.fallbackRoute = fallbackRoute;
        }

        Func<Type, ContextBag, Task<IUnicastRoute>> fallbackRoute = null;
        Dictionary<Type, IUnicastRoute> staticRoutes = new Dictionary<Type, IUnicastRoute>();
        static Task<IUnicastRoute> noRoute = Task.FromResult<IUnicastRoute>(null);
    }
}