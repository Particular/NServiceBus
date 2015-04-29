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
    public class StaticMessageRouter : IRouteMessages
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

		public bool SubscribeToPlainMessages { get; set; }

        public Address GetDestinationFor(Type messageType)
        {
            var address = GetMultiDestinationFor(messageType).FirstOrDefault();
        
            if (address == null)
            {
                return Address.Undefined;
            }

            return address;

        }

        internal List<Address> GetMultiDestinationFor(Type messageType)
        {
            List<Address> address;
            if (!routes.TryGetValue(messageType, out address))
            {
                return new List<Address>();
            }

            return address;
        }

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

        static readonly ILog Logger = LogManager.GetLogger(typeof(StaticMessageRouter));
        readonly ConcurrentDictionary<Type, List<Address>> routes;
    }
}
