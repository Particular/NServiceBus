namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using Logging;
    using Routing;
    using Unicast.Messages;

    class UnicastRoutingTable
    {
        UnicastRoutingTable(Dictionary<Type, List<IUnicastRoutingTableEntry>> tableEntries)
        {
            this.tableEntries = tableEntries;
        }

        public IEnumerable<UnicastRoutingTarget> Route(Type type, ContextBag contextBag)
        {
            List<IUnicastRoutingTableEntry> entries;
            return tableEntries.TryGetValue(type, out entries) 
                ? entries.SelectMany(e => e.GetTargets(contextBag)) 
                : emptyRoutes;
        }

        public static async Task<UnicastRoutingTable> Build(
            string name,
            Func<List<Type>, Task<IEnumerable<IUnicastRoute>>> routingTableConfiguration,
            EndpointInstances endpointInstances,
            DistributionPolicy distributionPolicy,
            List<Type> allMessageTypes,
            MessageMetadataRegistry messageMetadataRegistry)
        {
            var targetsByConcreteMessageType = await ComputeTargets(routingTableConfiguration, endpointInstances, allMessageTypes, messageMetadataRegistry).ConfigureAwait(false);
            var table = ComputeTableEntries(distributionPolicy, targetsByConcreteMessageType);
            LogTable(name, table);
            return new UnicastRoutingTable(table);
        }

        static void LogTable(string name, Dictionary<Type, List<IUnicastRoutingTableEntry>> table)
        {
            if (!log.IsDebugEnabled)
            {
                return;
            }
            foreach (var row in table)
            {
                log.DebugFormat("Unicast routing table {0}", name);
                log.DebugFormat(" * {0}", row.Key.FullName);
                foreach (var route in row.Value)
                {
                    log.DebugFormat("    - {0}", route);
                }
            }
        }

        static async Task<Dictionary<Type, List<UnicastRoutingTarget>>> ComputeTargets(Func<List<Type>, Task<IEnumerable<IUnicastRoute>>> routingTableConfiguration, EndpointInstances endpointInstances, List<Type> allMessageTypes, MessageMetadataRegistry messageMetadataRegistry)
        {
            var targetsByConcreteMessageType = new Dictionary<Type, List<UnicastRoutingTarget>>();

            foreach (var messageType in allMessageTypes)
            {
                var hierarchy = messageMetadataRegistry.GetMessageMetadata(messageType)
                    .MessageHierarchy
                    .Distinct()
                    .ToList();

                var routes = await routingTableConfiguration(hierarchy).ConfigureAwait(false);
                var destinations = new List<UnicastRoutingTarget>();
                foreach (var route in routes)
                {
                    var instances = await route.Resolve(endpointInstances.FindInstances).ConfigureAwait(false);
                    destinations.AddRange(instances);
                }
                targetsByConcreteMessageType[messageType] = destinations;
            }
            return targetsByConcreteMessageType;
        }

        static Dictionary<Type, List<IUnicastRoutingTableEntry>> ComputeTableEntries(DistributionPolicy distributionPolicy, Dictionary<Type, List<UnicastRoutingTarget>> targetsByConcreteMessageType)
        {
            var entriesByConcreteMessageType = new Dictionary<Type, List<IUnicastRoutingTableEntry>>();
            foreach (var entryData in targetsByConcreteMessageType)
            {
                var entries = new List<IUnicastRoutingTableEntry>();
                var entryDataByEndpoint = entryData.Value.GroupBy(e => e.Endpoint);
                foreach (var group in entryDataByEndpoint)
                {
                    if (@group.Key == null) //Routing targets that do not specify endpoint name
                    {
                        entries.AddRange(group.Select(t => new SingleUnicastRoutingTableEntry(t)));
                    }
                    else
                    {
                        var distributionStrategy = distributionPolicy.GetDistributionStrategy(entryData.Key);
                        var targets = @group.ToList();
                        entries.Add(new DistributedUnicastRoutingTableEntry(distributionStrategy.SelectDestination(targets), targets));
                    }
                }
                entriesByConcreteMessageType[entryData.Key] = entries;
            }
            return entriesByConcreteMessageType;
        }


        Dictionary<Type, List<IUnicastRoutingTableEntry>> tableEntries;
        static IEnumerable<UnicastRoutingTarget> emptyRoutes = Enumerable.Empty<UnicastRoutingTarget>();
        static ILog log = LogManager.GetLogger<UnicastRoutingTable>();
    }
}