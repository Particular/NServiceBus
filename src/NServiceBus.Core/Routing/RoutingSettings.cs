﻿namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Configuration.AdvanceExtensibility;
    using Routing;
    using Settings;
    using Transport;

    /// <summary>
    /// Exposes settings related to routing.
    /// </summary>
    public class RoutingSettings : ExposeSettings
    {
        internal RoutingSettings(SettingsHolder settings)
            : base(settings)
        {
        }

        /// <summary>
        /// Adds a static unicast route.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="destination">Destination endpoint.</param>
        public void RouteToEndpoint(Type messageType, string destination)
        {
            ThrowOnAddress(destination);

            Settings.GetOrCreate<UnicastRoutingTable>().RouteToEndpoint(messageType, destination);
        }

        /// <summary>
        /// Adds a static unicast route for all types contained in the specified assembly.
        /// </summary>
        /// <param name="assembly">The assembly whose messages should be routed.</param>
        /// <param name="destination">Destination endpoint.</param>
        public void RouteToEndpoint(Assembly assembly, string destination)
        {
            ThrowOnAddress(destination);

            var routingTable = Settings.GetOrCreate<UnicastRoutingTable>();
            foreach (var type in assembly.GetTypes())
            {
                routingTable.RouteToEndpoint(type, destination);
            }
        }

        /// <summary>
        /// Adds a static unicast route for all types contained in the specified assembly within the given namespace.
        /// </summary>
        /// <param name="assembly">The assembly whose messages should be routed.</param>
        /// <param name="messageNamespace">The namespace of the messages which should be routed.</param>
        /// <param name="destination">Destination endpoint.</param>
        public void RouteToEndpoint(Assembly assembly, string messageNamespace, string destination)
        {
            ThrowOnAddress(destination);

            // empty namespace is null, not string.empty
            messageNamespace = messageNamespace == string.Empty ? null : messageNamespace;

            var routingTable = Settings.GetOrCreate<UnicastRoutingTable>();
            foreach (var type in assembly.GetTypes().Where(t => t.Namespace == messageNamespace))
            {
                routingTable.RouteToEndpoint(type, destination);
            }
        }

        static void ThrowOnAddress(string destination)
        {
            if (destination.Contains("@"))
            {
                throw new ArgumentException($"A logical endpoint name should not contain '@', but received '{destination}'. To specify an endpoint's address, use the instance mapping file for the MSMQ transport, or refer to the routing documentation.");
            }
        }
    }

    /// <summary>
    /// Exposes settings related to routing.
    /// </summary>
    public class RoutingSettings<T> : RoutingSettings
        where T : TransportDefinition
    {
        internal RoutingSettings(SettingsHolder settings)
            : base(settings)
        {
        }
    }
}