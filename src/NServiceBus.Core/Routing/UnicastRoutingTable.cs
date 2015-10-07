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
        List<Func<Type, ContextBag, IEnumerable<UnicastRoutingDestination>>> rules = new List<Func<Type, ContextBag, IEnumerable<UnicastRoutingDestination>>>();

        internal IEnumerable<UnicastRoutingDestination> GetDestinationsFor(Type messageType, ContextBag contextBag)
        {
            return rules.SelectMany(r => r(messageType, contextBag)).Distinct();
        }

        /// <summary>
        /// Adds a static unicast route.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="destination">Destination endpoint.</param>
        public void AddStatic(Type messageType, EndpointName destination)
        {
            rules.Add((t, c) => StaticRule(t, messageType, new UnicastRoutingDestination(destination)));
        }


        /// <summary>
        /// Adds a static unicast route.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="destination">Destination endpoint instance.</param>
        public void AddStatic(Type messageType, EndpointInstanceName destination)
        {
            rules.Add((t, c) => StaticRule(t, messageType, new UnicastRoutingDestination(destination)));
        }


        /// <summary>
        /// Adds a static unicast route.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="destinationAddress">Destination endpoint instance address.</param>
        public void AddStatic(Type messageType, string destinationAddress)
        {
            rules.Add((t, c) => StaticRule(t, messageType, new UnicastRoutingDestination(destinationAddress)));
        }

        /// <summary>
        /// Adds a rule for generating unicast routes.
        /// </summary>
        /// <param name="dynamicRule">The rule.</param>
        public void AddDynamic(Func<Type, ContextBag, IEnumerable<UnicastRoutingDestination>> dynamicRule)
        {
            rules.Add(dynamicRule);
        }

        private static IEnumerable<UnicastRoutingDestination> StaticRule(Type messageBeingRouted, Type configuredMessage, UnicastRoutingDestination configuredDestination)
        {
            if (messageBeingRouted == configuredMessage)
            {
                yield return configuredDestination;
            }
        }
    }
}