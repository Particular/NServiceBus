namespace NServiceBus.Unicast.Routing
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using Logging;

    /// <summary>
    /// The default message router
    /// </summary>
    public class StaticMessageRouter : IRouteMessages
    {
        /// <summary>
        /// Initializes the router with all known messages
        /// </summary>
        /// <param name="knownMessages"></param>
        public StaticMessageRouter(IEnumerable<Type> knownMessages)
        {
            routes = new ConcurrentDictionary<Type, Address>();
            foreach (var knownMessage in knownMessages)
            {
                routes[knownMessage] = Address.Undefined;
            }
        }

        public Address GetDestinationFor(Type messageType)
        {
            Address address;
            if (!routes.TryGetValue(messageType, out address))
                return Address.Undefined;

            return address;

        }

        public void RegisterRoute(Type messageType, Address endpointAddress)
        {
            Address currentAddress;

            if (!routes.TryGetValue(messageType, out currentAddress))
                currentAddress = Address.Undefined;

            if(currentAddress == Address.Undefined)
            {
                Logger.DebugFormat("Routing for message: {0} set to {1}", messageType, endpointAddress); 
            }
            else
            {
                Logger.InfoFormat("Routing for message: {0} updated from {1} to {2}", messageType, currentAddress, endpointAddress);
            }

            routes[messageType] = endpointAddress;

            //go through the existing routes and see if this means that we can route any of those
            foreach (var route in routes)
            {
                if (messageType != route.Key && route.Key.IsAssignableFrom(messageType))
                {
                    if(route.Value == Address.Undefined)
                        Logger.DebugFormat("Routing for inherited message: {0}({1}) set to {2}",route.Key, messageType, endpointAddress);
                    else
                        Logger.InfoFormat("Routing for inherited message: {0}({1}) updated from {2} to {3}", route.Key, messageType,route.Value,endpointAddress);

                    routes[route.Key] = endpointAddress;
                }
            }

        }

        readonly IDictionary<Type, Address> routes;
        readonly static ILog Logger = LogManager.GetLogger(typeof(StaticMessageRouter));
    }
}