namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// The unicast subscriber table.
    /// </summary>
    public class UnicastSubscriberTable
    {
        static readonly UnicastRoute[] emptyResult = { };

        internal UnicastRoute[] GetRoutesFor(Type messageType)
        {
            UnicastRoute[] unicastRoutes;
            return routeTable.TryGetValue(messageType, out unicastRoutes)
                ? unicastRoutes
                : emptyResult;
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
            /*
            * The algorithm ensures that the last thread that updates (with side effects) the routeGroups value also updates the routeTable.
            * It does so by claiming a token when entering the code block. Only the thread with a current token updates routeTable. If the token is not valid any more
            * it means that there is another thread inside the method which came after it. The thread also spin-waits until all the threads that entered the method before it
            * exit to ensure that they don't overwrite the value it is going to put into routeTable
            *
            */
            IList<RouteTableEntry> existing = null;
            var newEntries = routeGroups.AddOrUpdate(sourceKey, entries, (key, e) =>
            {
                existing = e;
                return entries;
            });
            if (existing != null && existing.SequenceEqual(newEntries)) //No change for that key. We save some allocations.
            {
                return;
            }
            var enterToken = Interlocked.Increment(ref enterCounter);
            try
            {
                var newRouteTable = CalculateNewRouteTable();
                var current = Interlocked.CompareExchange(ref enterCounter, enterToken, enterToken);
                if (current == enterToken) //We still have valid token -- nobody else entered the method after us.
                {
                    //We need to wait until the previous thread exits
                    while (exitCounter != current)
                    {
                        //spin wait
                    }
                    routeTable = newRouteTable;
                }
            }
            finally
            {
                exitCounter = enterToken + 1; //Allow the next one to successfully write value
            }
        }

        Dictionary<Type, UnicastRoute[]> CalculateNewRouteTable()
        {
            var newRouteTable = new Dictionary<Type, List<UnicastRoute>>();
            foreach (var entry in routeGroups.Values.SelectMany(g => g))
            {
                List<UnicastRoute> typeRoutes;
                if (!newRouteTable.TryGetValue(entry.MessageType, out typeRoutes))
                {
                    typeRoutes = new List<UnicastRoute>();
                    newRouteTable[entry.MessageType] = typeRoutes;
                }
                typeRoutes.Add(entry.Route);
            }
            return newRouteTable.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());
        }

        Dictionary<Type, UnicastRoute[]> routeTable = new Dictionary<Type, UnicastRoute[]>();
        ConcurrentDictionary<object, IList<RouteTableEntry>> routeGroups = new ConcurrentDictionary<object, IList<RouteTableEntry>>();
        long enterCounter;
        long exitCounter = 1;
    }
}