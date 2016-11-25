namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// The unicast subscriber table.
    /// </summary>
    public class UnicastSubscriberTable
    {
        static readonly UnicastRoute[] emptyResult =
        {
        };

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
            // The algorithm uses ReaderWriterLockSlim. First entries are read. If then exists they are compared with passed entries and skipped if equal.
            // Otherwise, the write path is used. It's possible than one thread will execute all the work
            var existing = GetExistingRoutes(sourceKey);
            if (existing != null && existing.SequenceEqual(entries))
            {
                return;
            }

            readerWriterLock.EnterWriteLock();
            try
            {
                routeGroups[sourceKey] = entries;
                routeTable = CalculateNewRouteTable();
            }
            finally
            {
                readerWriterLock.ExitWriteLock();
            }
        }

        IList<RouteTableEntry> GetExistingRoutes(string sourceKey)
        {
            IList<RouteTableEntry> existing;
            readerWriterLock.EnterReadLock();
            try
            {
                routeGroups.TryGetValue(sourceKey, out existing);
            }
            finally
            {
                readerWriterLock.ExitReadLock();
            }
            return existing;
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

        volatile Dictionary<Type, UnicastRoute[]> routeTable = new Dictionary<Type, UnicastRoute[]>();
        Dictionary<string, IList<RouteTableEntry>> routeGroups = new Dictionary<string, IList<RouteTableEntry>>();
        ReaderWriterLockSlim readerWriterLock = new ReaderWriterLockSlim();
    }
}