namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Extensibility;

    /// <summary>
    /// Manages the direct routing table.
    /// </summary>
    public class DirectRoutingTable
    {
        List<Func<Type, ContextBag, IEnumerable<DirectRoutingDestination>>> rules = new List<Func<Type, ContextBag, IEnumerable<DirectRoutingDestination>>>();

        internal IEnumerable<DirectRoutingDestination> GetDestinationsFor(Type messageType, ContextBag contextBag)
        {
            return rules.SelectMany(r => r(messageType, contextBag)).Distinct();
        }

        /// <summary>
        /// Adds a static direct route.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="destination">Destination endpoint.</param>
        public void AddStatic(Type messageType, EndpointName destination)
        {
            rules.Add((t, c) => StaticRule(t, messageType, new DirectRoutingDestination(destination)));
        }


        /// <summary>
        /// Adds a static direct route.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="destination">Destination endpoint instance.</param>
        public void AddStatic(Type messageType, EndpointInstanceName destination)
        {
            rules.Add((t, c) => StaticRule(t, messageType, new DirectRoutingDestination(destination)));
        }


        /// <summary>
        /// Adds a static direct route.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="destinationAddress">Destination endpoint instance address.</param>
        public void AddStatic(Type messageType, string destinationAddress)
        {
            rules.Add((t, c) => StaticRule(t, messageType, new DirectRoutingDestination(destinationAddress)));
        }

        /// <summary>
        /// Adds a rule for generating direct routes.
        /// </summary>
        /// <param name="dynamicRule">The rule.</param>
        public void AddDynamic(Func<Type, ContextBag, IEnumerable<DirectRoutingDestination>> dynamicRule)
        {
            rules.Add(dynamicRule);
        }

        private static IEnumerable<DirectRoutingDestination> StaticRule(Type messageBeingRouted, Type configuredMessage, DirectRoutingDestination configuredDestination)
        {
            if (messageBeingRouted == configuredMessage)
            {
                yield return configuredDestination;
            }
        }
    }
}