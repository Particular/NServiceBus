namespace NServiceBus.Routing
{
    using System.Threading;

    /// <summary>
    /// Selects a single instance based on a round-robin scheme.
    /// </summary>
    public class SingleInstanceRoundRobinDistributionStrategy : DistributionStrategy
    {
        /// <summary>
        /// Selects destination instances from all known instances of a given endpoint.
        /// </summary>
        public override EndpointInstance SelectDestination(EndpointInstance[] currentAllInstances)
        {
            if (currentAllInstances.Length == 0)
            {
                return null;
            }
            var i = Interlocked.Increment(ref index);
            var result = currentAllInstances[(int)(i % currentAllInstances.Length)];
            return result;
        }

        // start with -1 so the index will be at 0 after the first increment.
        long index = -1;
    }
}