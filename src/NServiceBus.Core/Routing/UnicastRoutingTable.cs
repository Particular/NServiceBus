namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Manages the unicast routing table.
    /// </summary>
    public class UnicastRoutingTable
    {
        internal IUnicastRoute GetRouteFor(Type messageType)
        {
            IUnicastRoute unicastRoute;
            return routeTable.TryGetValue(messageType, out unicastRoute) 
                ? unicastRoute 
                : null;
        }

        /// <summary>
        /// Adds or replaces a group of routes for a given group key.
        /// </summary>
        /// <param name="sourceKey">Key for the route source.</param>
        /// <param name="entries">Group entries.</param>
        public void AddOrReplaceRoutes(object sourceKey, IList<RouteTableEntry> entries)
        {
            lock (updateLock)
            {
                routeGroups[sourceKey] = entries;
                var newRouteTableTemplate = new Dictionary<Type, RouteTableEntry>();
                foreach (var entry in routeGroups.Values.SelectMany(g => g))
                {
                    RouteTableEntry existing;
                    if (!newRouteTableTemplate.TryGetValue(entry.MessageType, out existing))
                    {
                        newRouteTableTemplate[entry.MessageType] = entry;
                    }
                    else
                    {
                        throw new Exception($"Route for type {entry.MessageType.FullName} already exists.");
                    }
                }
                var newRouteTable = newRouteTableTemplate.ToDictionary(e => e.Key, e => e.Value.Route);
                routeTable = newRouteTable;
            }
        }

        Dictionary<Type, IUnicastRoute> routeTable = new Dictionary<Type, IUnicastRoute>();
        Dictionary<object, IList<RouteTableEntry>> routeGroups = new Dictionary<object, IList<RouteTableEntry>>();
        object updateLock = new object();
    }
}