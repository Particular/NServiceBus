namespace NServiceBus.Unicast.Routing
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using Logging;

    /// <summary>
    ///     The default message router
    /// </summary>
    public class StaticMessageRouter
    {
        /// <summary>
        ///     Initializes the router with all known messages
        /// </summary>
        public StaticMessageRouter(IEnumerable<Type> knownMessages)
        {
            routes = new ConcurrentDictionary<Type, List<Address>>();
            foreach (var knownMessage in knownMessages)
            {
                routes[knownMessage] = new List<Address>();
            }
        }

        /// <summary>
        /// Set to true if the router should autosubscribe messages not defined as events
        /// </summary>
        public bool SubscribeToPlainMessages { get; set; }

        /// <summary>
        /// Returns all the routes for a given message
        /// </summary>
        /// <param name="messageType">The <see cref="Type"/> of the message to get the destination <see cref="Address"/> list for.</param>
        public List<Address> GetDestinationFor(Type messageType)
        {
            List<Address> address;
            if (!routes.TryGetValue(messageType, out address))
            {
                return new List<Address>();
            }

            return address;
        }

        /// <summary>
        /// Registers a route for the given event
        /// </summary>
        /// <param name="eventType">The <see cref="Type"/> of the event</param>
        /// <param name="endpointAddress">The <see cref="Address"/> representing the logical owner for the event</param>
        public void RegisterEventRoute(Type eventType, Address endpointAddress)
        {
            if (endpointAddress == null || endpointAddress == Address.Undefined)
            {
                throw new InvalidOperationException(String.Format("'{0}' can't be registered with Address.Undefined route.", eventType.FullName));
            }

            List<Address> currentAddress;

            if (!routes.TryGetValue(eventType, out currentAddress))
            {
                routes[eventType] = currentAddress = new List<Address>();
            }

            Logger.DebugFormat(currentAddress.Any() ? "Routing for message: {0} appending {1}" : "Routing for message: {0} set to {1}", eventType, endpointAddress);

            currentAddress.Add(endpointAddress);

            foreach (var route in routes.Where(route => eventType != route.Key && route.Key.IsAssignableFrom(eventType)))
            {
                if (route.Value.Any())
                {
                    Logger.InfoFormat("Routing for inherited message: {0}({1}) appending {2}", route.Key, eventType, endpointAddress);
                }
                else
                {
                    Logger.DebugFormat("Routing for inherited message: {0}({1}) set to {2}", route.Key, eventType, endpointAddress);
                }

                route.Value.Add(endpointAddress);
            }
        }

        /// <summary>
        /// Registers a route for the given message type
        /// </summary>
        /// <param name="messageType">The message type</param>
        /// <param name="endpointAddress">The address of the logical owner</param>
        public void RegisterMessageRoute(Type messageType, Address endpointAddress)
        {
            if (endpointAddress == null || endpointAddress == Address.Undefined)
            {
                throw new InvalidOperationException(String.Format("'{0}' can't be registered with Address.Undefined route.", messageType.FullName));
            }

            List<Address> currentAddress;

            if (!routes.TryGetValue(messageType, out currentAddress))
            {
                routes[messageType] = currentAddress = new List<Address>();
            }

            Logger.DebugFormat("Routing for message: {0} set to {1}", messageType, endpointAddress);
            currentAddress.Clear();
            currentAddress.Add(endpointAddress);

            //go through the existing routes and see if this means that we can route any of those
            foreach (var route in routes.Where(route => messageType != route.Key && route.Key.IsAssignableFrom(messageType)))
            {
                Logger.DebugFormat("Routing for inherited message: {0}({1}) set to {2}", route.Key, messageType, endpointAddress);
                route.Value.Clear();
                route.Value.Add(endpointAddress);
            }
        }

        static ILog Logger = LogManager.GetLogger<StaticMessageRouter>();
        readonly ConcurrentDictionary<Type, List<Address>> routes;
    }
}