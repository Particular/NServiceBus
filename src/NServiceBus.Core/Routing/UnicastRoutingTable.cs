namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;

    /// <summary>
    /// Manages the unicast routing table.
    /// </summary>
    public class UnicastRoutingTable
    {
        List<Func<Type, ContextBag, IUnicastRoute>> staticRules = new List<Func<Type, ContextBag, IUnicastRoute>>();
        List<Func<List<Type>, ContextBag, Task<IEnumerable<IUnicastRoute>>>> asyncDynamicRules = new List<Func<List<Type>, ContextBag, Task<IEnumerable<IUnicastRoute>>>>();
        List<Func<List<Type>, ContextBag, IEnumerable<IUnicastRoute>>> dynamicRules = new List<Func<List<Type>, ContextBag, IEnumerable<IUnicastRoute>>>();

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

            var staticRoutes = messageTypes
                .SelectMany(type => staticRules, (type, rule) => rule.Invoke(type, contextBag))
                .Where(route => route != null);

            routes.AddRange(staticRoutes);

            return routes;
        }

        /// <summary>
        /// Adds a static unicast route.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="destination">Destination endpoint.</param>
        public void RouteToEndpoint(Type messageType, Endpoint destination)
        {
            staticRules.Add((t, c) => StaticRule(t, messageType, new UnicastRoute(destination)));
        }
        
        /// <summary>
        /// Adds a static unicast route.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="destination">Destination endpoint.</param>
        public void RouteToEndpoint(Type messageType, string destination)
        {
            RouteToEndpoint(messageType, new Endpoint(destination));
        }

        /// <summary>
        /// Adds a static unicast route.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="destinationAddress">Destination endpoint instance address.</param>
        public void RouteToAddress(Type messageType, string destinationAddress)
        {
            staticRules.Add((t, c) => StaticRule(t, messageType, new UnicastRoute(destinationAddress)));
        }

        /// <summary>
        /// Adds a rule for generating unicast routes.
        /// </summary>
        /// <remarks>For dynamic routes that do not require async use <see cref="AddDynamic(Func{List{Type},ContextBag,IEnumerable{IUnicastRoute}})"/>.</remarks>
        /// <param name="dynamicRule">The rule.</param>
        public void AddDynamic(Func<List<Type>, ContextBag, Task<IEnumerable<IUnicastRoute>>> dynamicRule)
        {
            asyncDynamicRules.Add(dynamicRule);
        }

        /// <summary>
        /// Adds a rule for generating unicast routes.
        /// </summary>
        /// <remarks>For dynamic routes that require async use <see cref="AddDynamic(Func{List{Type},ContextBag,Task{IEnumerable{IUnicastRoute}}})"/>.</remarks>
        /// <param name="dynamicRule">The rule.</param>
        public void AddDynamic(Func<List<Type>, ContextBag, IEnumerable<IUnicastRoute>> dynamicRule)
        {
            dynamicRules.Add(dynamicRule);
        }

        static IUnicastRoute StaticRule(Type messageBeingRouted, Type configuredMessage, UnicastRoute configuredDestination)
        {
            return messageBeingRouted == configuredMessage ? configuredDestination : null;
        }
    }
}