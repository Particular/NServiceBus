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
        List<Func<Type, ContextBag, IEnumerable<IUnicastRoute>>> rules = new List<Func<Type, ContextBag, IEnumerable<IUnicastRoute>>>();

        internal IEnumerable<IUnicastRoute> GetDestinationsFor(Type messageType, ContextBag contextBag)
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
            rules.Add((t, c) => StaticRule(t, messageType, new UnicastRoute(destination)));
        }


        /// <summary>
        /// Adds a static unicast route.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="destination">Destination endpoint instance.</param>
        public void AddStatic(Type messageType, EndpointInstanceName destination)
        {
            rules.Add((t, c) => StaticRule(t, messageType, new UnicastRoute(destination)));
        }


        /// <summary>
        /// Adds a static unicast route.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="destinationAddress">Destination endpoint instance address.</param>
        public void AddStatic(Type messageType, string destinationAddress)
        {
            rules.Add((t, c) => StaticRule(t, messageType, new UnicastRoute(destinationAddress)));
        }

        /// <summary>
        /// Adds a rule for generating unicast routes.
        /// </summary>
        /// <param name="dynamicRule">The rule.</param>
        public void AddDynamic(Func<Type, ContextBag, IEnumerable<IUnicastRoute>> dynamicRule)
        {
            rules.Add(dynamicRule);
        }

        static IEnumerable<UnicastRoute> StaticRule(Type messageBeingRouted, Type configuredMessage, UnicastRoute configuredDestination)
        {
            if (messageBeingRouted == configuredMessage)
            {
                yield return configuredDestination;
            }
        }
    }
}