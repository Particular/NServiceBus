namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Manages the unicast routing table.
    /// </summary>
    public class UnicastRoutingTableConfiguration
    {
        internal async Task<IEnumerable<IUnicastRoute>> GetDestinationsFor(List<Type> messageTypeHierarchy)
        {
            var routes = new List<IUnicastRoute>();
            foreach (var rule in asyncDynamicRules)
            {
                routes.AddRange(await rule.Invoke(messageTypeHierarchy).ConfigureAwait(false));
            }

            foreach (var rule in dynamicRules)
            {
                routes.AddRange(rule.Invoke(messageTypeHierarchy));
            }

            var staticRoutes = messageTypeHierarchy
                .SelectMany(type => staticRules, (type, rule) => rule.Invoke(type))
                .Where(route => route != null);

            routes.AddRange(staticRoutes);

            return routes;
        }

        /// <summary>
        /// Adds a static unicast route.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="destination">Destination endpoint.</param>
        public void RouteToEndpoint(Type messageType, EndpointName destination)
        {
            staticRules.Add(t => StaticRule(t, messageType, new UnicastRoute(destination)));
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
            staticRules.Add(t => StaticRule(t, messageType, new UnicastRoute(destinationAddress)));
        }

        /// <summary>
        /// Adds an external provider of routes.
        /// </summary>
        /// <remarks>For dynamic routes that do not require async use <see cref="AddDynamic(System.Func{System.Collections.Generic.List{System.Type},System.Collections.Generic.IEnumerable{NServiceBus.Routing.IUnicastRoute}})"/>.</remarks>
        /// <param name="dynamicRule">The rule.</param>
        public void AddDynamic(Func<List<Type>, Task<IEnumerable<IUnicastRoute>>> dynamicRule)
        {
            asyncDynamicRules.Add(dynamicRule);
        }

        /// <summary>
        /// Adds an external provider of routes.
        /// </summary>
        /// <remarks>For dynamic routes that require async use <see cref="AddDynamic(System.Func{System.Collections.Generic.List{System.Type},System.Threading.Tasks.Task{System.Collections.Generic.IEnumerable{NServiceBus.Routing.IUnicastRoute}}})"/>.</remarks>
        /// <param name="dynamicRule">The rule.</param>
        public void AddDynamic(Func<List<Type>, IEnumerable<IUnicastRoute>> dynamicRule)
        {
            dynamicRules.Add(dynamicRule);
        }

        static IUnicastRoute StaticRule(Type messageBeingRouted, Type configuredMessage, UnicastRoute configuredDestination)
        {
            return messageBeingRouted == configuredMessage ? configuredDestination : null;
        }

        List<Func<List<Type>, Task<IEnumerable<IUnicastRoute>>>> asyncDynamicRules = new List<Func<List<Type>, Task<IEnumerable<IUnicastRoute>>>>();
        List<Func<List<Type>, IEnumerable<IUnicastRoute>>> dynamicRules = new List<Func<List<Type>, IEnumerable<IUnicastRoute>>>();
        List<Func<Type, IUnicastRoute>> staticRules = new List<Func<Type, IUnicastRoute>>();
    }
}