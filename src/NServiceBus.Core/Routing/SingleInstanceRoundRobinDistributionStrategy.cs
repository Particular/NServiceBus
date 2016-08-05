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
            var i = Interlocked.Increment(ref index);
            var result = currentAllInstances[(int)(i % currentAllInstances.Count)];
            yield return result;
        }

        // start with -1 so the index will be at 0 after the first increment.
        long index = -1;
    }
}