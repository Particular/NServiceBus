namespace NServiceBus.CentralizedRouting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    class CentralizedPubSubRoutingTable
    {
        public IEnumerable<string> GetSubscribersFor(Type[] messageTypes)
        {
            //Use caching
            return ComputeSubscribersFor(messageTypes).Distinct();
        }

        IEnumerable<string> ComputeSubscribersFor(Type[] messageTypes)
        {
            foreach (var type in messageTypes)
            {
                HashSet<string> subscribers;
                if (routeTable.TryGetValue(type, out subscribers))
                {
                    foreach (var subscriber in subscribers)
                    {
                        yield return subscriber;
                    }
                }
            }
        }

        public void ReplaceRoutes(IEnumerable<EndpointRoutingConfiguration> entries)
        {
            lock (updateLock)
            {
                var newRouteTable = new Dictionary<Type, HashSet<string>>();
                foreach (var entry in entries.Select(e => Tuple.Create(e.LogicalEndpointName, e.Events)))
                {
                    foreach (var eventType in entry.Item2)
                    {
                        HashSet<string> routesForEvent;
                        if (!newRouteTable.TryGetValue(eventType, out routesForEvent))
                        {
                            routesForEvent = new HashSet<string>();
                            newRouteTable[eventType] = routesForEvent;
                        }
                        routesForEvent.Add(entry.Item1);
                    }
                }
                routeTable = newRouteTable;
            }
        }

        Dictionary<Type, HashSet<string>> routeTable = new Dictionary<Type, HashSet<string>>();
        object updateLock = new object();
    }
}