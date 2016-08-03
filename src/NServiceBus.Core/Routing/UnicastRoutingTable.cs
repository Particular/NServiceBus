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
        internal Task<IEnumerable<IUnicastRoute>> GetDestinationsFor(Type messageType, ContextBag contextBag)
        {
            var routes = new List<IUnicastRoute>();

            IUnicastRoute messageRoutes;
            if (staticRoutes.TryGetValue(messageType, out messageRoutes))
            {
                routes.Add(messageRoutes);
            }

            foreach (var rule in dynamicRules)
            {
                routes.AddRange(rule.Invoke(messageType, contextBag));
            }

            if (asyncDynamicRules.Count > 0)
            {
                return AddAsyncDynamicRules(messageType, contextBag, routes);
            }

            if (routes.Count > 1)
            {
                throw new Exception($"Found ambiguous routes for message '{messageType.Name}'. Check your dynamic and static routes and avoid multiple routes for the same message type.");
            }

            return Task.FromResult<IEnumerable<IUnicastRoute>>(routes);
        }

        /// <summary>
        /// Adds a static unicast route for a given message type.
        /// </summary>
        /// /// <param name="messageType">The message type to use the route for.</param>
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
        /// <param name="overrideExistingRoute">Will override an existing route for the message type without throwing an exception when set to <code>true</code>.</param>
        /// <exception cref="Exception">Throws an exception when an ambiguous route exists and <paramref name="overrideExistingRoute"/> is not set to <code>true</code>.</exception>
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
        /// <remarks>For dynamic routes that do not require async use <see cref="AddDynamic(System.Func{Type,NServiceBus.Extensibility.ContextBag,System.Collections.Generic.IEnumerable{NServiceBus.Routing.IUnicastRoute}})" />.</remarks>
        /// <param name="dynamicRule">The rule.</param>
        public void AddDynamic(Func<Type, ContextBag, Task<IEnumerable<IUnicastRoute>>> dynamicRule)
        {
            asyncDynamicRules.Add(dynamicRule);
        }

        /// <summary>
        /// Adds an external provider of routes.
        /// </summary>
        /// <remarks>For dynamic routes that require async use <see cref="AddDynamic(System.Func{Type,NServiceBus.Extensibility.ContextBag,System.Threading.Tasks.Task{System.Collections.Generic.IEnumerable{NServiceBus.Routing.IUnicastRoute}}})" />.</remarks>
        /// <param name="dynamicRule">The rule.</param>
        public void AddDynamic(Func<Type, ContextBag, IEnumerable<IUnicastRoute>> dynamicRule)
        {
            dynamicRules.Add(dynamicRule);
        }

        async Task<IEnumerable<IUnicastRoute>> AddAsyncDynamicRules(Type messageType, ContextBag contextBag, List<IUnicastRoute> routes)
        {
            foreach (var rule in asyncDynamicRules)
            {
                routes.AddRange(await rule.Invoke(messageType, contextBag).ConfigureAwait(false));
            }

            if (routes.Count > 1)
            {
                throw new Exception($"Found ambiguous routes for message '{messageType.Name}'. Check your dynamic and static routes and avoid multiple routes for the same message type.");
            }

            return routes;
        }

        List<Func<Type, ContextBag, Task<IEnumerable<IUnicastRoute>>>> asyncDynamicRules = new List<Func<Type, ContextBag, Task<IEnumerable<IUnicastRoute>>>>();
        List<Func<Type, ContextBag, IEnumerable<IUnicastRoute>>> dynamicRules = new List<Func<Type, ContextBag, IEnumerable<IUnicastRoute>>>();
        Dictionary<Type, IUnicastRoute> staticRoutes = new Dictionary<Type, IUnicastRoute>();
    }
}