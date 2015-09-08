namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Extensibility;

    /// <summary>
    /// Manages the unicast routing table.
    /// </summary>
    public class UnicastRoutingTable
    {
        List<Func<Type, ContextBag, IEnumerable<UnicastRoutingRoute>>> rules = new List<Func<Type, ContextBag, IEnumerable<UnicastRoutingRoute>>>();

        internal IEnumerable<UnicastRoutingRoute> GetDestinationsFor(Type messageType, ContextBag contextBag)
        {
            return rules.SelectMany(r => r(messageType, contextBag)).Distinct();
        }

        /// <summary>
        /// Adds a static unicast route.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="route">Route.</param>
        public void AddStatic(Type messageType, UnicastRoutingRoute route)
        {
            rules.Add((t, c) => StaticRule(t, messageType, route));
        }

        /// <summary>
        /// Adds a static unicast route.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="destination">Destination endpoint.</param>
        public void AddStatic(Type messageType, EndpointName destination)
        {
            rules.Add((t, c) => StaticRule(t, messageType, new UnicastRoutingRoute(new UnicastRoutingDestination(destination))));
        }

        /// <summary>
        /// Adds a static unicast route.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="destination">Destination endpoint.</param>
        /// <param name="sendVia">Immediate dispatch instance (proxy).</param>
        public void AddStatic(Type messageType, EndpointName destination, EndpointInstanceName sendVia)
        {
            rules.Add((t, c) => StaticRule(t, messageType, new UnicastRoutingRoute(new UnicastRoutingDestination(destination), new DirectRoutingImmediateDestination(sendVia))));
        }

        /// <summary>
        /// Adds a static unicast route.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="destination">Destination endpoint.</param>
        /// <param name="sendVia">Immediate dispatch instance (proxy).</param>
        public void AddStatic(Type messageType, EndpointName destination, string sendVia)
        {
            rules.Add((t, c) => StaticRule(t, messageType, new UnicastRoutingRoute(new UnicastRoutingDestination(destination), new DirectRoutingImmediateDestination(sendVia))));
        }

        /// <summary>
        /// Adds a static unicast route.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="destination">Destination endpoint instance.</param>
        public void AddStatic(Type messageType, EndpointInstanceName destination)
        {
            rules.Add((t, c) => StaticRule(t, messageType, new UnicastRoutingRoute(new UnicastRoutingDestination(destination))));
        }


        /// <summary>
        /// Adds a static unicast route.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="destinationAddress">Destination endpoint instance address.</param>
        public void AddStatic(Type messageType, string destinationAddress)
        {
            rules.Add((t, c) => StaticRule(t, messageType, new UnicastRoutingRoute(new UnicastRoutingDestination(destinationAddress))));
        }

        /// <summary>
        /// Adds a static unicast route.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="destinationAddress">Destination endpoint instance address.</param>
        /// <param name="sendVia">Immediate dispatch instance (proxy).</param>
        public void AddStatic(Type messageType, string destinationAddress, string sendVia)
        {
            Guard.AgainstNull(nameof(messageType), messageType);
            Guard.AgainstNull(nameof(destinationAddress), destinationAddress);
            Guard.AgainstNull(nameof(sendVia), sendVia);
            rules.Add((t, c) => StaticRule(t, messageType, new UnicastRoutingRoute(new UnicastRoutingDestination(destinationAddress), new DirectRoutingImmediateDestination(sendVia))));
        }

        /// <summary>
        /// Adds a rule for generating unicast routes.
        /// </summary>
        /// <param name="dynamicRule">The rule.</param>
        public void AddDynamic(Func<Type, ContextBag, IEnumerable<UnicastRoutingRoute>> dynamicRule)
        {
            rules.Add(dynamicRule);
        }

        /// <summary>
        /// Adds a rule for generating unicast routes.
        /// </summary>
        /// <param name="dynamicRule">The rule.</param>
        public void AddDynamic(Func<Type, ContextBag, IEnumerable<UnicastRoutingDestination>> dynamicRule)
        {
            rules.Add((t, c) => dynamicRule(t, c).Select(d => new UnicastRoutingRoute(d)));
        }

        private static IEnumerable<UnicastRoutingRoute> StaticRule(Type messageBeingRouted, Type configuredMessage, UnicastRoutingRoute configuredRoute)
        {
            if (messageBeingRouted == configuredMessage)
            {
                yield return configuredRoute;
            }
        }
    }
}