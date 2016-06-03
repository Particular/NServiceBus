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
        internal async Task<IEnumerable<IUnicastRoute>> GetDestinationsFor(List<Type> messageTypes, ContextBag contextBag)
        {
            var routes = new List<IUnicastRoute>();
            foreach (var rule in asyncDynamicRules)
            {
                routes.AddRange(await rule.Invoke(messageTypes, contextBag).ConfigureAwait(false));
            }

            foreach (var rule in dynamicRules)
            {
                routes.AddRange(rule.Invoke(messageTypes, contextBag));
            }

            foreach (var messageType in messageTypes)
            {
                List<IUnicastRoute> messageRoutes;
                if (staticRoutes.TryGetValue(messageType, out messageRoutes))
                {
                    routes.AddRange(messageRoutes);
                }
            }

            return routes;
        }

        /// <summary>
        /// Adds a static unicast route.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="destination">Destination endpoint.</param>
        public void RouteToEndpoint(Type messageType, EndpointName destination)
        {
            AddStaticRoute(messageType, new UnicastRoute(destination));
        }

        /// <summary>
        /// Adds a static unicast route.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="destination">Destination endpoint.</param>
        public void RouteToEndpoint(Type messageType, string destination)
        {
            RouteToEndpoint(messageType, new EndpointName(destination));
        }

        /// <summary>
        /// Adds a static unicast route.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="destinationAddress">Destination endpoint instance address.</param>
        public void RouteToAddress(Type messageType, string destinationAddress)
        {
            AddStaticRoute(messageType, new UnicastRoute(destinationAddress));
        }

        /// <summary>
        /// Adds an external provider of routes.
        /// </summary>
        /// <remarks>For dynamic routes that do not require async use <see cref="AddDynamic(System.Func{System.Collections.Generic.List{System.Type},NServiceBus.Extensibility.ContextBag,System.Collections.Generic.IEnumerable{NServiceBus.Routing.IUnicastRoute}})"/>.</remarks>
        /// <param name="dynamicRule">The rule.</param>
        public void AddDynamic(Func<List<Type>, ContextBag, Task<IEnumerable<IUnicastRoute>>> dynamicRule)
        {
            asyncDynamicRules.Add(dynamicRule);
        }

        /// <summary>
        /// Adds an external provider of routes.
        /// </summary>
        /// <remarks>For dynamic routes that require async use <see cref="AddDynamic(System.Func{System.Collections.Generic.List{System.Type},NServiceBus.Extensibility.ContextBag,System.Threading.Tasks.Task{System.Collections.Generic.IEnumerable{NServiceBus.Routing.IUnicastRoute}}})"/>.</remarks>
        /// <param name="dynamicRule">The rule.</param>
        public void AddDynamic(Func<List<Type>, ContextBag, IEnumerable<IUnicastRoute>> dynamicRule)
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

        List<Func<List<Type>, ContextBag, Task<IEnumerable<IUnicastRoute>>>> asyncDynamicRules = new List<Func<List<Type>, ContextBag, Task<IEnumerable<IUnicastRoute>>>>();
        List<Func<List<Type>, ContextBag, IEnumerable<IUnicastRoute>>> dynamicRules = new List<Func<List<Type>, ContextBag, IEnumerable<IUnicastRoute>>>();
        Dictionary<Type, List<IUnicastRoute>> staticRoutes = new Dictionary<Type, List<IUnicastRoute>>();
    }
}