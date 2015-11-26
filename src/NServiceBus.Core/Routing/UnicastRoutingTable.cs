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
        List<Func<Type, ContextBag, Task<List<IUnicastRoute>>>> dynamicRules = new List<Func<Type, ContextBag, Task<List<IUnicastRoute>>>>();

        internal async Task<IEnumerable<IUnicastRoute>> GetDestinationsFor(Type messageType, ContextBag contextBag)
        {
            var dynamicRoutes = new List<IUnicastRoute>();
            foreach (var rule in dynamicRules)
            {
                dynamicRoutes.AddRange(await rule.Invoke(messageType, contextBag).ConfigureAwait(false));
            }

            var staticRoutes = staticRules.Select(rule => rule.Invoke(messageType, contextBag))
                                          .Where(routeFromStaticRule => routeFromStaticRule != null);

            return dynamicRoutes.Concat(staticRoutes).Distinct();
        }

        /// <summary>
        /// Adds a static unicast route.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="destination">Destination endpoint.</param>
        public void AddStatic(Type messageType, EndpointName destination)
        {
            staticRules.Add((t, c) => StaticRule(t, messageType, new UnicastRoute(destination)));
        }


        /// <summary>
        /// Adds a static unicast route.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="destination">Destination endpoint instance.</param>
        public void AddStatic(Type messageType, EndpointInstanceName destination)
        {
            staticRules.Add((t, c) => StaticRule(t, messageType, new UnicastRoute(destination)));
        }


        /// <summary>
        /// Adds a static unicast route.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="destinationAddress">Destination endpoint instance address.</param>
        public void AddStatic(Type messageType, string destinationAddress)
        {
            staticRules.Add((t, c) => StaticRule(t, messageType, new UnicastRoute(destinationAddress)));
        }

        /// <summary>
        /// Adds a rule for generating unicast routes.
        /// </summary>
       // /// <param name="dynamicRule">The rule.</param>
        public void AddDynamic(Func<Type, ContextBag, Task<List<IUnicastRoute>>> dynamicRule)
        {
            dynamicRules.Add(dynamicRule);
        }

        static IUnicastRoute StaticRule(Type messageBeingRouted, Type configuredMessage, UnicastRoute configuredDestination)
        {
            return messageBeingRouted == configuredMessage ? configuredDestination : null;
        }
    }
}