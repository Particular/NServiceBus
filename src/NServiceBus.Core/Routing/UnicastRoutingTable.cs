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
        internal Task<IEnumerable<IUnicastRoute>> GetDestinationsFor(Type[] messageTypes, ContextBag contextBag)
        {
            var routes = new List<IUnicastRoute>();

            foreach (var messageType in messageTypes)
            {
                List<IUnicastRoute> messageRoutes;
                if (staticRoutes.TryGetValue(messageType, out messageRoutes))
                {
                    routes.AddRange(messageRoutes);
                }
            }

            foreach (var rule in dynamicRules)
            {
                routes.AddRange(rule.Invoke(messageTypes, contextBag));
            }

            if (asyncDynamicRules.Count > 0)
            {
                return AddAsyncDynamicRules(messageTypes, contextBag, routes);
            }

            return Task.FromResult<IEnumerable<IUnicastRoute>>(routes);
        }

        /// <summary>
        /// Adds a static unicast route to a logical endpoint.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="destination">Destination endpoint.</param>
        public void RouteToEndpoint(Type messageType, string destination)
        {
            AddStaticRoute(messageType, UnicastRoute.CreateFromEndpointName(destination));
        }

        /// <summary>
        /// Adds a static unicast route to a specific endpoint instance.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="instance">Destination endpoint instance.</param>
        public void RouteToInstance(Type messageType, EndpointInstance instance)
        {
            AddStaticRoute(messageType, UnicastRoute.CreateFromEndpointInstance(instance));
        }

        /// <summary>
        /// Adds a static unicast route to a specific transport address.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="destinationAddress">Destination endpoint instance address.</param>
        public void RouteToAddress(Type messageType, string destinationAddress)
        {
            AddStaticRoute(messageType, UnicastRoute.CreateFromPhysicalAddress(destinationAddress));
        }

        /// <summary>
        /// Adds an external provider of routes.
        /// </summary>
        /// <remarks>For dynamic routes that do not require async use <see cref="AddDynamic(System.Func{Type[],NServiceBus.Extensibility.ContextBag,System.Collections.Generic.IEnumerable{NServiceBus.Routing.IUnicastRoute}})" />.</remarks>
        /// <param name="dynamicRule">The rule.</param>
        public void AddDynamic(Func<Type[], ContextBag, Task<IEnumerable<IUnicastRoute>>> dynamicRule)
        {
            asyncDynamicRules.Add(dynamicRule);
        }

        /// <summary>
        /// Adds an external provider of routes.
        /// </summary>
        /// <remarks>For dynamic routes that require async use <see cref="AddDynamic(System.Func{Type[],NServiceBus.Extensibility.ContextBag,System.Threading.Tasks.Task{System.Collections.Generic.IEnumerable{NServiceBus.Routing.IUnicastRoute}}})" />.</remarks>
        /// <param name="dynamicRule">The rule.</param>
        public void AddDynamic(Func<Type[], ContextBag, IEnumerable<IUnicastRoute>> dynamicRule)
        {
            dynamicRules.Add(dynamicRule);
        }

        void AddStaticRoute(Type messageType, IUnicastRoute route)
        {
            List<IUnicastRoute> existingRoutes;
            if (staticRoutes.TryGetValue(messageType, out existingRoutes))
            {
                existingRoutes.Add(route);
            }
            else
            {
                staticRoutes.Add(messageType, new List<IUnicastRoute>
                {
                    route
                });
            }
        }

        async Task<IEnumerable<IUnicastRoute>> AddAsyncDynamicRules(Type[] messageTypes, ContextBag contextBag, List<IUnicastRoute> routes)
        {
            foreach (var rule in asyncDynamicRules)
            {
                routes.AddRange(await rule.Invoke(messageTypes, contextBag).ConfigureAwait(false));
            }

            return routes;
        }

        List<Func<Type[], ContextBag, Task<IEnumerable<IUnicastRoute>>>> asyncDynamicRules = new List<Func<Type[], ContextBag, Task<IEnumerable<IUnicastRoute>>>>();
        List<Func<Type[], ContextBag, IEnumerable<IUnicastRoute>>> dynamicRules = new List<Func<Type[], ContextBag, IEnumerable<IUnicastRoute>>>();
        Dictionary<Type, List<IUnicastRoute>> staticRoutes = new Dictionary<Type, List<IUnicastRoute>>();
    }
}