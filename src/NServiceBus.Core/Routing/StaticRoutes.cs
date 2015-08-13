namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    class StaticRoutes
    {
        public void Register(Type messageType, string address)
        {
            Guard.AgainstNull("messageType", messageType);
            Guard.AgainstNullAndEmpty("address", address);

            routes[messageType] = address;
        }

        public bool TryGet(Type messageType, out string address)
        {
            return routes.TryGetValue(messageType, out address);
        }

        Dictionary<Type, string> routes = new Dictionary<Type, string>();

        public IEnumerable<StaticRoute> GetAllRoutes()
        {
            return routes.Select(kvp => new StaticRoute(kvp.Key, kvp.Value));
        }

        public class StaticRoute
        {
            public StaticRoute(Type messageType, string address)
            {
                MessageType = messageType;
                Address = address;
            }

            public Type MessageType { get; private set; }
            public string Address { get; private set; }
        }
    }
}