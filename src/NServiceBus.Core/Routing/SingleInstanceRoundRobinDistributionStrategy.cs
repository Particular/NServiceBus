namespace NServiceBus.Routing
{
    using System.Collections.Generic;
    using System.Threading;

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
                yield break;
            }
            var result = currentAllInstances[(int)(index % currentAllInstances.Count)];
            Interlocked.Increment(ref index);
            yield return result;
        }

        long index;
    }
}