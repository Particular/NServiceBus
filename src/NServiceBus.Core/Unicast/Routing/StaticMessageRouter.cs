namespace NServiceBus.Unicast.Routing
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using Logging;

    /// <summary>
    /// The default message router
    /// </summary>
    public class StaticMessageRouter 
    {
        /// <summary>
        /// Initializes the router with all known messages
        /// </summary>
        /// <param name="knownMessages"></param>
        public StaticMessageRouter(IEnumerable<Type> knownMessages)
        {
            routes = new ConcurrentDictionary<Type, List<Address>>();
            foreach (var knownMessage in knownMessages)
            {
                routes[knownMessage] = new List<Address>();
            }
        }

        public List<Address> GetDestinationFor(Type messageType)
        {
            List<Address> address;
            if (!routes.TryGetValue(messageType, out address))
            {
                return new List<Address>();
            }

            return address;

        }

        public void RegisterRoute(Type messageType, Address endpointAddress)
        {
            if(endpointAddress == null || endpointAddress == Address.Undefined)
                throw new InvalidOperationException("Undefined routes are not allowed, MessageType: " + messageType.FullName);

            List<Address> currentAddress;

            if (!routes.TryGetValue(messageType, out currentAddress))
            {
                routes[messageType]= currentAddress = new List<Address>();
            }

            if (currentAddress.Any())
            {
                Logger.InfoFormat("Routing for message: {0} appending {1}", messageType, endpointAddress);
            }
            else
            {
                Logger.DebugFormat("Routing for message: {0} set to {1}", messageType, endpointAddress);
            }

            currentAddress.Add(endpointAddress);

            //go through the existing routes and see if this means that we can route any of those
            foreach (var route in routes)
            {
                if (messageType != route.Key && route.Key.IsAssignableFrom(messageType))
                {
                    if (route.Value.Any())
                    {
                        Logger.InfoFormat("Routing for inherited message: {0}({1}) appending {2}", route.Key, messageType, endpointAddress);
                    }
                    else
                    {
                        Logger.DebugFormat("Routing for inherited message: {0}({1}) set to {2}", route.Key, messageType, endpointAddress);
                    }

                    route.Value.Add(endpointAddress);
                }
            }

        }

        ConcurrentDictionary<Type, List<Address>> routes;
        static ILog Logger = LogManager.GetLogger(typeof(StaticMessageRouter));
    }
}