namespace NServiceBus.Routing
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Selects a single instance based on a round-robin scheme.
    /// </summary>
    public class SingleInstanceRoundRobinDistributionStrategy : DistributionStrategy
    {
        /// <summary>
        /// Selects destination instances from all known instances of a given endpoint.
        /// </summary>
        public override IEnumerable<UnicastRoutingTarget> SelectDestination(IList<UnicastRoutingTarget> currentAllInstances)
        {
            if (currentAllInstances.Count == 0)
            {
                return Enumerable.Empty<UnicastRoutingTarget>();
            }
            var endpointName = currentAllInstances[0].Endpoint;
            var index = indexes.AddOrUpdate(endpointName, e => 0L, (e, i) => i + 1L);

            return new[]
            {
                currentAllInstances[(int) (index%currentAllInstances.Count)]
            };
        }

        ConcurrentDictionary<string, long> indexes = new ConcurrentDictionary<string, long>();
    }
}