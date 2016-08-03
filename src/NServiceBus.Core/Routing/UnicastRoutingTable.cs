namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;

    /// <summary>
    /// Manages the unicast routing table.
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

            foreach (var rule in dynamicRules)
            {
                var route = rule(messageType, contextBag);
                if (route != null)
                {
                    return Task.FromResult(route);
                }
            }

            if (asyncDynamicRules.Count > 0)
            {
                return ExecuteDynamicRules(messageType, contextBag);
            }

            return noRoute;
        }

        /// <summary>
        /// Adds a static unicast route for a given message type.
        /// </summary>
        /// ///
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
        /// Adds an external provider of routes.
        /// </summary>
        /// <remarks>
        /// For dynamic routes that do not require async use
        /// <see cref="AddDynamic(System.Func{Type,NServiceBus.Extensibility.ContextBag,NServiceBus.Routing.IUnicastRoute})" />.
        /// </remarks>
        /// <param name="dynamicRule">The rule.</param>
        public void AddDynamic(Func<Type, ContextBag, Task<IUnicastRoute>> dynamicRule)
        {
            asyncDynamicRules.Add(dynamicRule);
        }

        /// <summary>
        /// Adds an external provider of routes.
        /// </summary>
        /// <remarks>
        /// For dynamic routes that require async use
        /// <see
        ///     cref="AddDynamic(System.Func{Type,NServiceBus.Extensibility.ContextBag,System.Threading.Tasks.Task{NServiceBus.Routing.IUnicastRoute}})" />
        /// .
        /// </remarks>
        /// <param name="dynamicRule">The rule.</param>
        public void AddDynamic(Func<Type, ContextBag, IUnicastRoute> dynamicRule)
        {
            dynamicRules.Add(dynamicRule);
        }

        async Task<IUnicastRoute> ExecuteDynamicRules(Type messageType, ContextBag contextBag)
        {
            foreach (var rule in asyncDynamicRules)
            {
                var route = await rule(messageType, contextBag).ConfigureAwait(false);
                if (route != null)
                {
                    return route;
                }
            }

            return null;
        }

        List<Func<Type, ContextBag, Task<IUnicastRoute>>> asyncDynamicRules = new List<Func<Type, ContextBag, Task<IUnicastRoute>>>();
        List<Func<Type, ContextBag, IUnicastRoute>> dynamicRules = new List<Func<Type, ContextBag, IUnicastRoute>>();
        Dictionary<Type, IUnicastRoute> staticRoutes = new Dictionary<Type, IUnicastRoute>();
        static Task<IUnicastRoute> noRoute = Task.FromResult<IUnicastRoute>(null);
    }
}