namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The unicast routing table.
    /// </summary>
    public class UnicastRoutingTable
    {
        internal UnicastRoute GetRouteFor(Type messageType)
        {
            UnicastRoute unicastRoute;
            return routeTable.TryGetValue(messageType, out unicastRoute)
                ? unicastRoute
                : null;
        }

        /// <summary>
        /// Adds or replaces a set of routes for a given group key. The route set is identified <paramref name="sourceKey"></paramref>.
        /// If the method is called the first time with a given <paramref name="sourceKey"></paramref>, the routes are added.
        /// If the method is called with the same <paramref name="sourceKey"></paramref> multiple times, the routes registered previously under this key are replaced.
        /// </summary>
        /// <param name="sourceKey">Key for the route source.</param>
        /// <param name="entries">Group entries.</param>
        public void AddOrReplaceRoutes(string sourceKey, IList<RouteTableEntry> entries)
        {
            lock (updateLock)
            {
                routeGroups[sourceKey] = entries;
                var newRouteTable = new Dictionary<Type, UnicastRoute>();
                foreach (var entry in routeGroups.Values.SelectMany(g => g))
                {
                    if (newRouteTable.ContainsKey(entry.MessageType))
                    {
                        throw new Exception($"Route for type {entry.MessageType.FullName} already exists.");
                    }
                    newRouteTable[entry.MessageType] = entry.Route;
                }
                routeTable = newRouteTable;
            }
        }

        Dictionary<Type, UnicastRoute> routeTable = new Dictionary<Type, UnicastRoute>();
        Dictionary<object, IList<RouteTableEntry>> routeGroups = new Dictionary<object, IList<RouteTableEntry>>();
        object updateLock = new object();
    }
}