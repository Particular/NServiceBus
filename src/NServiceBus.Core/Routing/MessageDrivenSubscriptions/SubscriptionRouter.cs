namespace NServiceBus.Routing.MessageDrivenSubscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    class SubscriptionRouter
    {
        public SubscriptionRouter(StaticRoutes staticRoutes, ICollection<Type> messageTypes)
        {
            routes = new Dictionary<Type, List<string>>();

            foreach (var route in staticRoutes.GetAllRoutes())
            {
                AddRoute(route);

                var route1 = route;

                //find inherited types that would also match this route
                foreach (var messageType in messageTypes.Where(t => t != route1.MessageType && t.IsAssignableFrom(route1.MessageType)))
                {
                    AddRoute(messageType, route1.Address);
                }
            }
        }

        void AddRoute(StaticRoutes.StaticRoute route)
        {
            AddRoute(route.MessageType, route.Address);
        }

        void AddRoute(Type messageType, string address)
        {
            List<string> addresses;

            if (!routes.TryGetValue(messageType, out addresses))
            {
                addresses = new List<string>();
                routes[messageType] = addresses;
            }

            addresses.Add(address);
        }

        public IEnumerable<string> GetAddressesForEventType(Type messageType)
        {
            List<string> addresses;

            if (!routes.TryGetValue(messageType, out addresses))
            {
                return new List<string>();
            }


            return addresses;
        }

        Dictionary<Type, List<string>> routes;
    }
}