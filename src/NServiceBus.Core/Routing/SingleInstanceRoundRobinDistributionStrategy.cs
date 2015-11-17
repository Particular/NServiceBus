namespace NServiceBus.Routing
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Selects a single instance based on a round-robin scheme.
    /// </summary>
    public class SingleInstanceRoundRobinDistributionStrategy : DistributionStrategy
    {
        volatile IList<UnicastRoutingTarget> allInstances;
        long index;
        object lockObject = new object();

        /// <summary>
        /// Selects destination instances from all known instances of a given endpoint.
        /// </summary>
        public override IEnumerable<UnicastRoutingTarget> SelectDestination(IEnumerable<UnicastRoutingTarget> currentAllInstances)
        {
            var localAllInstances = allInstances;
            var currentList = currentAllInstances.ToList();
            if (localAllInstances == null || !currentList.SequenceEqual(localAllInstances))
            {
                lock (lockObject)
                {
                    localAllInstances = allInstances;
                    if (localAllInstances == null || !currentList.SequenceEqual(localAllInstances))
                    {
                        allInstances = currentList;
                        localAllInstances = currentList;
                        index = 0;
                    }
                }
            }
            var arrayIndex = index%localAllInstances.Count;
            var destination = allInstances[(int)arrayIndex];
            index++;
            yield return destination;
        }
    }
}